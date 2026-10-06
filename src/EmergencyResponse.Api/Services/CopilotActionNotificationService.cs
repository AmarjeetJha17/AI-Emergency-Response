using EmergencyResponse.Api.Hubs;
using EmergencyResponse.Api.Realtime;
using EmergencyResponse.Application.Copilot.Actions;
using Microsoft.AspNetCore.SignalR;

namespace EmergencyResponse.Api.Services;

public sealed class CopilotActionNotificationService
    : ICopilotActionNotificationService
{
    private readonly IHubContext<IncidentHub> _hub;

    public CopilotActionNotificationService(IHubContext<IncidentHub> hub)
    {
        _hub = hub;
    }

    public Task PublishAsync(
        CopilotActionUpdatedEvent update,
        string? proposerUserId,
        CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hub.Clients.Group(SignalRGroups.DashboardOperators)
                .SendAsync("CopilotActionUpdated", update, cancellationToken)
        };
        if (!string.IsNullOrWhiteSpace(proposerUserId))
        {
            tasks.Add(
                _hub.Clients.User(proposerUserId)
                    .SendAsync("CopilotActionUpdated", update, cancellationToken));
        }

        return Task.WhenAll(tasks);
    }
}
