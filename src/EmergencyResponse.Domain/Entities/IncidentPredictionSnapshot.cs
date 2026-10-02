using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Predictive;

namespace EmergencyResponse.Domain.Entities;

public sealed class IncidentPredictionSnapshot : BaseEntity
{
    public Guid IncidentId { get; set; }

    public PredictionType PredictionType { get; set; }

    public DateTime SnapshotAtUtc { get; set; }

    public string FeatureSnapshotJson { get; set; } = "{}";

    public bool? Label { get; set; }

    public DateTime LabelEvaluationAtUtc { get; set; }

    public DateTime? OutcomeAtUtc { get; set; }

    public Guid? OutcomeEscalationId { get; set; }

    public string FeatureHash { get; set; } = string.Empty;
}
