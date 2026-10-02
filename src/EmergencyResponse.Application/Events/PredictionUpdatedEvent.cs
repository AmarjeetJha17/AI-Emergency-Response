namespace EmergencyResponse.Application.Events;

public sealed class PredictionUpdatedEvent
{
    public Guid IncidentId { get; init; }

    public Guid PredictionId { get; init; }

    public string PredictionType { get; init; } = "EscalationRisk";

    public bool PredictedEscalation { get; init; }

    public double Probability { get; init; }

    public double RiskScore { get; init; }

    public string ModelName { get; init; } = string.Empty;

    public string ModelVersion { get; init; } = string.Empty;

    public DateTime GeneratedAtUtc { get; init; }
}
