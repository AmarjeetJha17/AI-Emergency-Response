namespace EmergencyResponse.Application.Predictive;

public sealed class EscalationRiskPrediction
{
    public Guid IncidentId { get; init; }

    public bool PredictedEscalation { get; init; }

    public double Probability { get; init; }

    public double RiskScore { get; init; }

    public string ModelName { get; init; } = string.Empty;

    public string ModelVersion { get; init; } = string.Empty;

    public string DatasetVersion { get; init; } = string.Empty;

    public string FeatureSchemaVersion { get; init; } = string.Empty;

    public DateTime GeneratedAtUtc { get; init; }

    public DateTime? ValidUntilUtc { get; init; }

    public string Status { get; init; } = "Completed";

    public string? FailureReason { get; init; }

    public string? FeatureSnapshotJson { get; init; }

    public IReadOnlyDictionary<string, double> SignalsConsidered { get; init; }
        = new Dictionary<string, double>();
}
