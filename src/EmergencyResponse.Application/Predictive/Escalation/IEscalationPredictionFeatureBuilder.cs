namespace EmergencyResponse.Application.Predictive.Escalation;

public interface IEscalationPredictionFeatureBuilder
{
    Task<PredictionFeatureSnapshot> BuildSnapshotAsync(
        Guid incidentId,
        DateTime snapshotAtUtc,
        CancellationToken cancellationToken = default);
}
