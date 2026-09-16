using EmergencyResponse.Application.Events;

namespace EmergencyResponse.Application.Interfaces;

public interface IIncidentNotificationService
{
    Task NotifyIncidentUpdatedAsync(
        IncidentUpdatedEvent notification,
        CancellationToken cancellationToken = default);
}