using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public sealed class OutboxMessage : BaseEntity
{
    public string MessageType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string MessageKey { get; set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; }
    public DateTimeOffset? PublishedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? LastAttemptAtUtc { get; set; }
    public string? LastError { get; set; }
    public bool IsPublished { get; set; }
}
