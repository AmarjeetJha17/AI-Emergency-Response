using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Domain.Entities;

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

    Task NotifyEscalationUpdatedAsync(
        EscalationUpdatedEvent eventData,
        CancellationToken cancellationToken = default);

    Task NotifySituationUpdatedAsync(
        SituationUpdatedEvent eventData,
        CancellationToken cancellationToken = default);

    Task NotifyNotificationCreatedAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}