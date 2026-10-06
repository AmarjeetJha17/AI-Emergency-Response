using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Application.Notifications;

namespace EmergencyResponse.Infrastructure.Copilot.Tools;

public sealed class NotificationCopilotTools
{
    private readonly INotificationService _service;
    private readonly CopilotToolAuthorization _authorization;

    public NotificationCopilotTools(
        INotificationService service,
        CopilotToolAuthorization authorization)
    {
        _service = service;
        _authorization = authorization;
    }

    public async Task<CopilotToolResult<IReadOnlyList<CopilotNotificationResult>>> GetRecentNotificationsAsync(
        int take = 10,
        CancellationToken cancellationToken = default)
    {
        _authorization.RequireAuthenticated();
        take = CopilotToolValidation.Take(take, 20);
        var notifications = await _service.GetForCurrentUserAsync(
            take,
            cancellationToken);
        return CopilotToolResult<IReadOnlyList<CopilotNotificationResult>>.Available(
            notifications.Select(x => new CopilotNotificationResult
            {
                Id = x.Id,
                Type = x.TypeName,
                Priority = x.PriorityName,
                Status = x.StatusName,
                Title = x.Title,
                Message = x.Message,
                IncidentId = x.IncidentId,
                IncidentNumber = x.IncidentNumber,
                SituationId = x.OperationalSituationId,
                SituationNumber = x.SituationNumber,
                IsRead = x.IsRead,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToList());
    }
}
