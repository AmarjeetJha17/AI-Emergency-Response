using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Domain.Copilot;

namespace EmergencyResponse.Infrastructure.Copilot.Tools;

public sealed class ActionCopilotTools
{
    private readonly ICopilotActionService _actions;
    private readonly ICopilotExecutionContext _context;
    private readonly CopilotToolAuthorization _authorization;

    public ActionCopilotTools(
        ICopilotActionService actions,
        ICopilotExecutionContext context,
        CopilotToolAuthorization authorization)
    {
        _actions = actions;
        _context = context;
        _authorization = authorization;
    }

    public Task<CopilotActionProposalResult> ProposeAddIncidentNoteAsync(
        Guid incidentId,
        string content,
        string reason,
        CancellationToken cancellationToken = default) =>
        ProposeAsync(
            CopilotActionType.CreateIncidentNote,
            incidentId,
            new Dictionary<string, object?>
            {
                ["content"] = content
            },
            "Add incident note",
            $"{reason.Trim()}: {content.Trim()}",
            cancellationToken);

    public Task<CopilotActionProposalResult> ProposeAddIncidentTagAsync(
        Guid incidentId,
        string tag,
        string reason,
        CancellationToken cancellationToken = default) =>
        ProposeAsync(
            CopilotActionType.AddIncidentTag,
            incidentId,
            new Dictionary<string, object?>
            {
                ["tag"] = tag
            },
            "Add incident tag",
            $"{reason.Trim()}: {tag.Trim()}",
            cancellationToken);

    private async Task<CopilotActionProposalResult> ProposeAsync(
        CopilotActionType actionType,
        Guid incidentId,
        Dictionary<string, object?> arguments,
        string title,
        string description,
        CancellationToken cancellationToken)
    {
        CopilotToolValidation.RequiredId(incidentId, nameof(incidentId));
        await _authorization.RequireIncidentAccessAsync(
            incidentId,
            cancellationToken);
        if (description.Length > 2000)
        {
            throw new ArgumentException(
                "The action description is too long.",
                nameof(description));
        }

        var proposal = await _actions.ProposeAsync(
            _context.InteractionId,
            new CopilotActionProposalRequest
            {
                ActionType = actionType,
                Title = title,
                Description = description,
                Arguments = arguments,
                IncidentId = incidentId
            },
            cancellationToken);

        return new CopilotActionProposalResult(
            proposal.Id,
            proposal.ActionType,
            proposal.Status,
            proposal.RiskLevel,
            proposal.Status == CopilotActionStatus.AwaitingApproval,
            $"Action proposed and requires explicit approval: {proposal.Title}.",
            proposal.ExpiresAtUtc);
    }
}
