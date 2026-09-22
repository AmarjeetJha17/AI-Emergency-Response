using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Realtime;

namespace EmergencyResponse.Application.Interfaces;

public interface IIncidentNotificationService
{
    Task NotifyIncidentUpdatedAsync(
        IncidentUpdatedEvent notification,
        CancellationToken cancellationToken = default);

    Task NotifyDashboardUpdatedAsync(
        DashboardUpdatedEvent eventData,
        CancellationToken cancellationToken = default);

    Task NotifyResourceUpdatedAsync(
        ResourceUpdatedEvent eventData,
        CancellationToken cancellationToken = default);
}