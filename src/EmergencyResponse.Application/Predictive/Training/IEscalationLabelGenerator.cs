namespace EmergencyResponse.Application.Predictive.Training;

public interface IEscalationLabelGenerator
{
    Task<EscalationLabelResult> GenerateAsync(
        Guid incidentId,
        DateTime snapshotAtUtc,
        CancellationToken cancellationToken = default);
}
