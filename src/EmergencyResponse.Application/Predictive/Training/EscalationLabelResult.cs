namespace EmergencyResponse.Application.Predictive.Training;

public sealed class EscalationLabelResult
{
    public bool Label { get; init; }

    public DateTime? OutcomeAtUtc { get; init; }

    public Guid? OutcomeEscalationId { get; init; }
}
