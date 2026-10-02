using System.Text.Json;
using EmergencyResponse.Application.Predictive.Escalation;
using EmergencyResponse.Application.Predictive.Training;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Predictive;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Predictive.Training;

public sealed class IncidentPredictionSnapshotBuilder
    : IIncidentPredictionSnapshotBuilder
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEscalationPredictionFeatureBuilder _featureBuilder;

    public IncidentPredictionSnapshotBuilder(
        ApplicationDbContext dbContext,
        IEscalationPredictionFeatureBuilder featureBuilder)
    {
        _dbContext = dbContext;
        _featureBuilder = featureBuilder;
    }

    public async Task<IncidentPredictionSnapshot> BuildAsync(
        Guid incidentId,
        DateTime snapshotAtUtc,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.IncidentPredictionSnapshots
            .AsNoTracking()
            .Where(x =>
                x.IncidentId == incidentId &&
                x.PredictionType == PredictionType.EscalationRisk &&
                x.SnapshotAtUtc == snapshotAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        if (snapshotAtUtc < DateTime.UtcNow.AddMinutes(-5))
        {
            throw new InvalidOperationException(
                "Historical snapshot is unavailable because current-only incident fields " +
                "cannot be reconstructed safely. Capture snapshots at prediction time first.");
        }

        var liveSnapshot = await _featureBuilder.BuildSnapshotAsync(
            incidentId,
            snapshotAtUtc,
            cancellationToken);

        var entity = new IncidentPredictionSnapshot
        {
            Id = Guid.NewGuid(),
            IncidentId = incidentId,
            PredictionType = PredictionType.EscalationRisk,
            SnapshotAtUtc = snapshotAtUtc,
            FeatureSnapshotJson = JsonSerializer.Serialize(liveSnapshot.Features),
            LabelEvaluationAtUtc = DateTime.UtcNow,
            FeatureHash = liveSnapshot.FeatureHash
        };

        _dbContext.IncidentPredictionSnapshots.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
