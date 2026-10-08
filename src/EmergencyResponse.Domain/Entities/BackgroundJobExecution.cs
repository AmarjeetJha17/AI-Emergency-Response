using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public sealed class BackgroundJobExecution : BaseEntity
{
    public Guid JobId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string AnalysisVersion { get; set; } = string.Empty;
    public Guid WorkItemId { get; set; }
    public BackgroundJobStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? LastFailureCategory { get; set; }
}
