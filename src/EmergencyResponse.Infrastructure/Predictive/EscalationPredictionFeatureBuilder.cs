using System.Text.Json;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.Predictive.Escalation;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.Predictive;

public sealed class EscalationPredictionFeatureBuilder : IEscalationPredictionFeatureBuilder
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<EscalationPredictionFeatureBuilder> _logger;

    public EscalationPredictionFeatureBuilder(
        ApplicationDbContext dbContext,
        ILogger<EscalationPredictionFeatureBuilder> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PredictionFeatureSnapshot> BuildSnapshotAsync(
        Guid incidentId,
        DateTime snapshotAtUtc,
        CancellationToken cancellationToken = default)
    {
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .Include(i => i.AIAnalyses)
            .FirstOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null)
        {
            throw new KeyNotFoundException($"Incident {incidentId} not found.");
        }

        // Facts extracted from latest completed AI analysis
        EmergencyFacts? facts = null;
        var latestAnalysis = incident.AIAnalyses
            .Where(a => a.CreatedAtUtc <= snapshotAtUtc)
            .OrderByDescending(a => a.CreatedAtUtc)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(latestAnalysis?.ExtractedFactsJson))
        {
            try
            {
                facts = JsonSerializer.Deserialize<EmergencyFacts>(latestAnalysis.ExtractedFactsJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse ExtractedFactsJson for incident {IncidentId}", incidentId);
            }
        }

        // Count escalations that occurred at or before snapshot time (no future leakage!)
        var escalationCount = await _dbContext.IncidentEscalations
            .AsNoTracking()
            .CountAsync(e => e.IncidentId == incidentId && e.CreatedAtUtc <= snapshotAtUtc, cancellationToken);

        // Count duplicates identified at or before snapshot time
        var duplicateCount = await _dbContext.IncidentDuplicateMatches
            .AsNoTracking()
            .CountAsync(d => (d.SourceIncidentId == incidentId || d.CandidateIncidentId == incidentId) &&
                             d.CreatedAtUtc <= snapshotAtUtc, cancellationToken);

        // Count related incidents in shared situation
        var situationIncidents = await _dbContext.OperationalSituationIncidents
            .AsNoTracking()
            .Where(si => si.IncidentId == incidentId && si.AddedAtUtc <= snapshotAtUtc)
            .Select(si => si.OperationalSituationId)
            .ToListAsync(cancellationToken);

        int relatedCount = 0;
        bool resourceShortage = false;

        if (situationIncidents.Count > 0)
        {
            relatedCount = await _dbContext.OperationalSituationIncidents
                .AsNoTracking()
                .CountAsync(si => situationIncidents.Contains(si.OperationalSituationId) &&
                                  si.IncidentId != incidentId &&
                                  si.AddedAtUtc <= snapshotAtUtc, cancellationToken);

            resourceShortage = await _dbContext.OperationalSituations
                .AsNoTracking()
                .AnyAsync(s => situationIncidents.Contains(s.Id) && s.ResourceShortageDetected, cancellationToken);
        }

        var ageMinutes = (float)Math.Max(0, (snapshotAtUtc - incident.ReportedAtUtc).TotalMinutes);

        var features = new EscalationPredictionFeatures
        {
            SeverityScore = incident.SeverityScore,
            PriorityScore = incident.PriorityScore,
            PeopleAffected = incident.PeopleAffected ?? 0,
            VehiclesAffected = incident.VehiclesAffected ?? 0,
            Casualties = incident.Casualties ?? 0,
            ImmediateDanger = facts?.ImmediateDanger == true ? 1f : 0f,
            PeopleTrapped = facts?.PeopleTrapped == true ? 1f : 0f,
            CriticalInfrastructureAffected = facts?.CriticalInfrastructureAffected == true ? 1f : 0f,
            InfrastructureDamage = facts?.InfrastructureDamage == true ? 1f : 0f,
            RoadBlocked = facts?.RoadBlocked == true ? 1f : 0f,
            EscalationCount = escalationCount,
            DuplicateCount = duplicateCount,
            RelatedIncidentCount = relatedCount,
            ResourceShortage = resourceShortage ? 1f : 0f,
            IncidentAgeMinutes = ageMinutes
        };

        var featureHash = PredictionFeatureSnapshot.ComputeFeatureHash(features);

        return new PredictionFeatureSnapshot
        {
            IncidentId = incidentId,
            SnapshotAtUtc = snapshotAtUtc,
            FeatureHash = featureHash,
            Features = features
        };
    }
}
