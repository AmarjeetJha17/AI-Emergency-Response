using EmergencyResponse.Domain.Notifications;

namespace EmergencyResponse.Application.Notifications.DTOs;

public sealed record NotificationItem
{
    public Guid Id { get; init; }

    public NotificationType Type { get; init; }

    public string TypeName => Type.ToString();

    public NotificationPriority Priority { get; init; }

    public string PriorityName => Priority.ToString();

    public NotificationStatus Status { get; init; }

    public string StatusName => Status.ToString();

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public Guid? IncidentId { get; init; }

    public string? IncidentNumber { get; init; }

    public Guid? OperationalSituationId { get; init; }

    public string? SituationNumber { get; init; }

    public Guid? IncidentEscalationId { get; init; }

    public bool IsRead { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? ReadAtUtc { get; init; }

    public string? TargetRole { get; init; }

    public string? ActionUrl { get; init; }
}
