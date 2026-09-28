using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class AIAnalysis : BaseEntity
{
    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public string ModelName { get; set; } = null!;

    public string? ModelVersion { get; set; }

    public string PromptVersion { get; set; } = null!;

    public string InputReference { get; set; } = null!;

    public string OutputJson { get; set; } = null!;

    public double? Confidence { get; set; }

    public long ExecutionTimeMs { get; set; }

    public bool HumanReviewed { get; set; }

    public string? HumanDecision { get; set; }

    public AIAnalysisStatus Status { get; set; }

    public string? FailureReason { get; set; }
    public string? ExtractedFactsJson { get; set; }

    public int AttemptNumber { get; set; } = 1;
    public int RetryCount { get; set; } = 0;
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? NextRetryAtUtc { get; set; }
    public string? FailureCategory { get; set; }

    public int? InputTokens { get; set; }
    public int? OutputTokens { get; set; }
    public int? TotalTokens { get; set; }
}