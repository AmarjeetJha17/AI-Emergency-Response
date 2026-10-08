using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class AuditLog : BaseEntity
{
    public EmergencyResponse.Domain.Enums.DataClassification DataClassification { get; set; } = EmergencyResponse.Domain.Enums.DataClassification.Restricted;
    public string? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityName { get; set; } = null!;

    public string EntityId { get; set; } = null!;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? IpAddress { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
