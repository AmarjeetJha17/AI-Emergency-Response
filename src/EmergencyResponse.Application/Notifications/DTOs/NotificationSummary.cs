namespace EmergencyResponse.Application.Notifications.DTOs;

public sealed record NotificationSummary
{
    public int TotalUnread { get; init; }

    public int ImportantUnread { get; init; }

    public int UrgentUnread { get; init; }

    public int CriticalUnread { get; init; }
}
