using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class VisualAnalysis : BaseEntity
{
    public Guid IncidentMediaId { get; set; }

    public IncidentMedia IncidentMedia { get; set; } = null!;

    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public string ModelName { get; set; } = null!;

    public string? ModelVersion { get; set; }

    public string PromptVersion { get; set; } = null!;

    public VisualAnalysisStatus Status { get; set; }

    public string? FindingsJson { get; set; }

    public string? RawOutputJson { get; set; }

    public double? Confidence { get; set; }

    public long? ExecutionTimeMs { get; set; }

    public bool HumanReviewed { get; set; }

    public string? HumanDecision { get; set; }

    public string? FailureReason { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public int? FramesAnalyzed { get; set; }

    public double? VideoDurationSeconds { get; set; }

    public string? FrameTimestampsJson { get; set; }

    /// <summary>
    /// JSON array of storage keys for extracted frame images, in frame order.
    /// Null for image analyses or when frames were not persisted.
    /// </summary>
    public string? FrameStorageKeysJson { get; set; }
}