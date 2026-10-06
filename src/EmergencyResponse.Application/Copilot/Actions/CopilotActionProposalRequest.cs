using EmergencyResponse.Domain.Copilot;

namespace EmergencyResponse.Application.Copilot.Actions;

public sealed class CopilotActionProposalRequest
{
    public CopilotActionType ActionType { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Dictionary<string, object?> Arguments { get; init; } = [];

    public Guid? IncidentId { get; init; }

    public Guid? SituationId { get; init; }
}
