using System.Security.Claims;
using EmergencyResponse.Api.Realtime;
using EmergencyResponse.Application.Common.Roles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EmergencyResponse.Api.Hubs;

[Authorize]
public sealed class IncidentHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        var departmentId = Context.User?.FindFirst("DepartmentId")?.Value;
        if (!string.IsNullOrEmpty(departmentId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"department:{departmentId}");
        }

        if (Context.User?.IsInRole(ApplicationRoles.Operator) == true ||
            Context.User?.IsInRole(ApplicationRoles.EmergencyAdmin) == true ||
            Context.User?.IsInRole(ApplicationRoles.SystemAdmin) == true)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                SignalRGroups.DashboardOperators);
        }
        else if (Context.User?.IsInRole(ApplicationRoles.Responder) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{ApplicationRoles.Responder}");
        }
        else if (Context.User?.IsInRole(ApplicationRoles.Citizen) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{ApplicationRoles.Citizen}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userId}");
        }

        var departmentId = Context.User?.FindFirst("DepartmentId")?.Value;
        if (!string.IsNullOrEmpty(departmentId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"department:{departmentId}");
        }

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            SignalRGroups.DashboardOperators);

        await base.OnDisconnectedAsync(exception);
    }
}