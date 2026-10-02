using System.Text.Json;
using EmergencyResponse.Application.Predictive.Escalation;
using EmergencyResponse.Application.Predictive.Training;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.PredictiveTraining.Training;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.PredictiveTraining.Data;

public sealed class HistoricalDatasetExporter
{
    public async Task<HistoricalDatasetExportResult> ExportAsync(
        string connectionString,
        string datasetVersion,
        int predictionWindowHours,
        CancellationToken cancellationToken = default)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var dbContext = new ApplicationDbContext(options);
        var latestSnapshot = await dbContext.IncidentPredictionSnapshots
            .AsNoTracking()
            .MaxAsync(x => (DateTime?)x.SnapshotAtUtc, cancellationToken);
        var latestEscalation = await dbContext.IncidentEscalations
            .AsNoTracking()
            .MaxAsync(x => (DateTime?)x.CreatedAtUtc, cancellationToken);

        var dataCutoffUtc = new[] { latestSnapshot, latestEscalation }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .DefaultIfEmpty(DateTime.UtcNow)
            .Max();
        var latestUsableSnapshot = dataCutoffUtc.AddHours(-predictionWindowHours);

        var snapshots = await dbContext.IncidentPredictionSnapshots
            .Where(x =>
                x.PredictionType == EmergencyResponse.Domain.Predictive.PredictionType.EscalationRisk &&
                x.SnapshotAtUtc <= latestUsableSnapshot)
            .OrderBy(x => x.SnapshotAtUtc)
            .ToListAsync(cancellationToken);

        var rows = new List<EscalationModelInput>(snapshots.Count);
        foreach (var snapshot in snapshots)
        {
            var escalation = await dbContext.IncidentEscalations
                .AsNoTracking()
                .Where(x =>
                    x.IncidentId == snapshot.IncidentId &&
                    x.CreatedAtUtc > snapshot.SnapshotAtUtc &&
                    x.CreatedAtUtc <= snapshot.SnapshotAtUtc.AddHours(predictionWindowHours))
                .OrderBy(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            var features = JsonSerializer.Deserialize<EscalationPredictionFeatures>(
                snapshot.FeatureSnapshotJson);
            if (features is null)
            {
                continue;
            }

            snapshot.Label = escalation is not null;
            snapshot.OutcomeAtUtc = escalation?.CreatedAtUtc;
            snapshot.OutcomeEscalationId = escalation?.Id;
            snapshot.LabelEvaluationAtUtc = DateTime.UtcNow;

            rows.Add(new EscalationModelInput
            {
                SnapshotAtUtc = snapshot.SnapshotAtUtc,
                IncidentId = snapshot.IncidentId,
                OutcomeAtUtc = escalation?.CreatedAtUtc,
                FeatureHash = snapshot.FeatureHash,
                SeverityScore = features.SeverityScore,
                PriorityScore = features.PriorityScore,
                PeopleAffected = features.PeopleAffected,
                VehiclesAffected = features.VehiclesAffected,
                Casualties = features.Casualties,
                ImmediateDanger = features.ImmediateDanger,
                PeopleTrapped = features.PeopleTrapped,
                CriticalInfrastructureAffected = features.CriticalInfrastructureAffected,
                InfrastructureDamage = features.InfrastructureDamage,
                RoadBlocked = features.RoadBlocked,
                EscalationCount = features.EscalationCount,
                DuplicateCount = features.DuplicateCount,
                RelatedIncidentCount = features.RelatedIncidentCount,
                ResourceShortage = features.ResourceShortage,
                IncidentAgeMinutes = features.IncidentAgeMinutes,
                Label = escalation is not null
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new HistoricalDatasetExportResult
        {
            Rows = rows,
            DataCutoffUtc = dataCutoffUtc
        };
    }

    public static TrainingDatasetMetadata CreateMetadata(
        IReadOnlyList<EscalationModelInput> rows,
        string datasetVersion,
        int predictionWindowHours)
    {
        return new TrainingDatasetMetadata
        {
            DatasetVersion = datasetVersion,
            GeneratedAtUtc = DateTime.UtcNow,
            MinSnapshotAtUtc = rows.Min(x => x.SnapshotAtUtc),
            MaxSnapshotAtUtc = rows.Max(x => x.SnapshotAtUtc),
            TotalRows = rows.Count,
            PositiveRows = rows.Count(x => x.Label),
            NegativeRows = rows.Count(x => !x.Label),
            FeatureSchemaVersion = FeatureSchemaVersions.EscalationRiskV1,
            PredictionWindowHours = predictionWindowHours
        };
    }
}
