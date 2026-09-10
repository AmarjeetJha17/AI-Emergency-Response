using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}