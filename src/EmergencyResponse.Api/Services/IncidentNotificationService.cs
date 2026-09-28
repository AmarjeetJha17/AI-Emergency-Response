using EmergencyResponse.Api.Hubs;
using EmergencyResponse.Api.Realtime;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Domain.Entities;
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

    public async Task NotifyResourceUpdatedAsync(
        ResourceUpdatedEvent eventData,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group(SignalRGroups.DashboardOperators)
            .SendAsync(
                "ResourceUpdated",
                eventData,
                cancellationToken);
    }

    public async Task NotifyEscalationUpdatedAsync(
        EscalationUpdatedEvent eventData,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group(SignalRGroups.DashboardOperators)
            .SendAsync(
                "EscalationUpdated",
                eventData,
                cancellationToken);
    }

    public async Task NotifySituationUpdatedAsync(
        SituationUpdatedEvent eventData,
        CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients
            .Group(SignalRGroups.DashboardOperators)
            .SendAsync(
                "SituationUpdated",
                eventData,
                cancellationToken);
    }

    public async Task NotifyNotificationCreatedAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        var evt = new NotificationCreatedEvent
        {
            NotificationId = notification.Id,
            Type = notification.Type,
            Priority = notification.Priority,
            Title = notification.Title,
            Message = notification.Message,
            IncidentId = notification.IncidentId,
            OperationalSituationId = notification.OperationalSituationId,
            IncidentEscalationId = notification.IncidentEscalationId,
            ActionUrl = notification.IncidentId != null ? $"/Incidents/Details/{notification.IncidentId}" :
                        notification.OperationalSituationId != null ? $"/Situations/Details/{notification.OperationalSituationId}" :
                        "/Dashboard",
            CreatedAtUtc = notification.CreatedAtUtc
        };

        if (!string.IsNullOrWhiteSpace(notification.UserId))
        {
            await _hubContext.Clients
                .Group($"user:{notification.UserId}")
                .SendAsync("NotificationCreated", evt, cancellationToken);
        }
        else if (!string.IsNullOrWhiteSpace(notification.TargetRole))
        {
            if (notification.TargetRole is ApplicationRoles.Operator or ApplicationRoles.EmergencyAdmin or ApplicationRoles.SystemAdmin)
            {
                await _hubContext.Clients
                    .Group(SignalRGroups.DashboardOperators)
                    .SendAsync("NotificationCreated", evt, cancellationToken);
            }
            else
            {
                await _hubContext.Clients
                    .Group($"role:{notification.TargetRole}")
                    .SendAsync("NotificationCreated", evt, cancellationToken);
            }
        }
        else if (notification.TargetDepartmentId.HasValue)
        {
            await _hubContext.Clients
                .Group($"department:{notification.TargetDepartmentId}")
                .SendAsync("NotificationCreated", evt, cancellationToken);
        }
        else
        {
            await _hubContext.Clients
                .Group(SignalRGroups.DashboardOperators)
                .SendAsync("NotificationCreated", evt, cancellationToken);
        }
    }
}