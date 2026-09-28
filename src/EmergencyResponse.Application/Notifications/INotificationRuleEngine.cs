using EmergencyResponse.Application.Notifications.DTOs;

namespace EmergencyResponse.Application.Notifications;

public interface INotificationRuleEngine
{
    IReadOnlyList<NotificationRequest> Evaluate(NotificationContext context);
}
