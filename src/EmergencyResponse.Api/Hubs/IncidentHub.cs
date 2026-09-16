using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EmergencyResponse.Api.Hubs;

[Authorize]
public sealed class IncidentHub : Hub
{
}