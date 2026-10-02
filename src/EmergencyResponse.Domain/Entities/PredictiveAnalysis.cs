using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Predictive;

namespace EmergencyResponse.Domain.Entities;

public sealed class PredictiveAnalysis : BaseEntity
{
    public Guid IncidentId { get; set; }

    public PredictionType PredictionType { get; set; }

    public PredictionStatus Status { get; set; }

    public string ModelName { get; set; } = string.Empty;

    public string ModelVersion { get; set; } = string.Empty;

    public string DatasetVersion { get; set; } = string.Empty;

    public string FeatureSchemaVersion { get; set; } = string.Empty;

    public string FeatureSnapshotJson { get; set; } = "{}";

    public string FeatureProvenanceJson { get; set; } = "{}";

    public string? PredictionJson { get; set; }

    public double? Probability { get; set; }

    public double? RiskScore { get; set; }

    public bool? PredictedEscalation { get; set; }

    public DateTime GeneratedAtUtc { get; set; }

    public DateTime? ValidUntilUtc { get; set; }

    public bool HumanReviewed { get; set; }

    public string? HumanDecision { get; set; }

    public string? FailureReason { get; set; }
}
