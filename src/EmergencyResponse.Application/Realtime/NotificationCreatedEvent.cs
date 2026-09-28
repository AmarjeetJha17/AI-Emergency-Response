using EmergencyResponse.Domain.Notifications;

namespace EmergencyResponse.Application.Realtime;

public sealed record NotificationCreatedEvent
{
    public Guid NotificationId { get; init; }

    public NotificationType Type { get; init; }

    public string TypeName => Type.ToString();

    public NotificationPriority Priority { get; init; }

    public string PriorityName => Priority.ToString();

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public Guid? IncidentId { get; init; }

    public Guid? OperationalSituationId { get; init; }

    public Guid? IncidentEscalationId { get; init; }

    public string? ActionUrl { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
