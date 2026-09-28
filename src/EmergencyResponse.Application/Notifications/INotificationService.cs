using EmergencyResponse.Application.Notifications.DTOs;

namespace EmergencyResponse.Application.Notifications;

public interface INotificationService
{
    Task<NotificationItem?> CreateAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationItem>> GetForCurrentUserAsync(
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<NotificationSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(
        CancellationToken cancellationToken = default);
}
