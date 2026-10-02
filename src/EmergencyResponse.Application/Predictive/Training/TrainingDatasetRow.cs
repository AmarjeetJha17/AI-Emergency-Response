using EmergencyResponse.Application.Predictive.Escalation;

namespace EmergencyResponse.Application.Predictive.Training;

public sealed class TrainingDatasetRow
{
    public DateTime SnapshotAtUtc { get; init; }

    public EscalationPredictionFeatures Features { get; init; } = new();

    public bool Label { get; init; }

    public DateTime? OutcomeAtUtc { get; init; }

    public Guid IncidentId { get; init; }

    public string FeatureHash { get; init; } = string.Empty;
}
