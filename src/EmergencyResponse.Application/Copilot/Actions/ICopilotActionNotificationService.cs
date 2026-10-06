using EmergencyResponse.Domain.Copilot;

namespace EmergencyResponse.Application.Copilot.Actions;

public sealed record CopilotActionUpdatedEvent(
    Guid ProposalId,
    CopilotActionType ActionType,
    CopilotActionStatus Status,
    Guid? IncidentId,
    DateTime UpdatedAtUtc);

public interface ICopilotActionNotificationService
{
    Task PublishAsync(
        CopilotActionUpdatedEvent update,
        string? proposerUserId,
        CancellationToken cancellationToken = default);
}
