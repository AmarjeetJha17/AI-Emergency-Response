using EmergencyResponse.Application.Notifications.DTOs;

namespace EmergencyResponse.Web.Services;

public interface INotificationApiClient
{
    Task<IReadOnlyList<NotificationItem>> GetForCurrentUserAsync(
        int take = 50,
        CancellationToken cancellationToken = default);

    Task<NotificationSummary?> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(
        CancellationToken cancellationToken = default);
}
