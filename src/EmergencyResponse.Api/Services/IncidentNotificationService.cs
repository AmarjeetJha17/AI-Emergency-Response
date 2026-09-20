using EmergencyResponse.Api.Hubs;
using EmergencyResponse.Api.Realtime;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Realtime;

using Microsoft.AspNetCore.SignalR;

namespace EmergencyResponse.Api.Services;

public sealed class IncidentNotificationService
    : IIncidentNotificationService
{
    private readonly IHubContext<IncidentHub> _hubContext;

    public IncidentNotificationService(
        IHubContext<IncidentHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyIncidentUpdatedAsync(
        IncidentUpdatedEvent notification,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .All
            .SendAsync(
                "IncidentUpdated",
                notification,
                cancellationToken);
    }

    public async Task NotifyDashboardUpdatedAsync(
        DashboardUpdatedEvent eventData,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group(SignalRGroups.DashboardOperators)
            .SendAsync(
                "DashboardUpdated",
                eventData,
                cancellationToken);
    }
}