using EmergencyResponse.Domain.Notifications;

namespace EmergencyResponse.Application.Notifications.DTOs;

public sealed record NotificationRequest
{
    public NotificationType Type { get; init; }

    public NotificationPriority Priority { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? UserId { get; init; }

    public string? TargetRole { get; init; }

    public Guid? TargetDepartmentId { get; init; }

    public Guid? IncidentId { get; init; }

    public Guid? OperationalSituationId { get; init; }

    public Guid? IncidentEscalationId { get; init; }

    public string? DeduplicationKey { get; init; }

    public DateTime? ExpiresAtUtc { get; init; }

    public string? MetadataJson { get; init; }
}
