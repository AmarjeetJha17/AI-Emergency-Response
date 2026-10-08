using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Notifications;

namespace EmergencyResponse.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public EmergencyResponse.Domain.Enums.DataClassification DataClassification { get; set; } = EmergencyResponse.Domain.Enums.DataClassification.Confidential;
    public string? UserId { get; set; }

    public string? TargetRole { get; set; }

    public Guid? TargetDepartmentId { get; set; }

    public NotificationType Type { get; set; }

    public NotificationPriority Priority { get; set; }

    public NotificationStatus Status { get; set; } = NotificationStatus.Unread;

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public Guid? IncidentId { get; set; }

    public Incident? Incident { get; set; }

    public Guid? OperationalSituationId { get; set; }

    public OperationalSituation? OperationalSituation { get; set; }

    public Guid? IncidentEscalationId { get; set; }

    public IncidentEscalation? IncidentEscalation { get; set; }

    public bool IsRead { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public string? DeduplicationKey { get; set; }

    public string? MetadataJson { get; set; }
}
