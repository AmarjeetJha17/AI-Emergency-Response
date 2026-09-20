using EmergencyResponse.Api.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EmergencyResponse.Api.Hubs;

[Authorize]
public sealed class IncidentHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        if (Context.User?.IsInRole("Operator") == true ||
            Context.User?.IsInRole("EmergencyAdmin") == true ||
            Context.User?.IsInRole("SystemAdmin") == true)
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                SignalRGroups.DashboardOperators);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            SignalRGroups.DashboardOperators);

        await base.OnDisconnectedAsync(exception);
    }
}