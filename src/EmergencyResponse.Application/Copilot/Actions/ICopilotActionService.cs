using EmergencyResponse.Domain.Entities;

namespace EmergencyResponse.Application.Copilot.Actions;

public interface ICopilotActionService
{
    Task<CopilotActionProposal> ProposeAsync(
        Guid copilotInteractionId,
        CopilotActionProposalRequest request,
        CancellationToken cancellationToken = default);

    Task<CopilotActionProposal> ApproveAsync(
        Guid proposalId,
        string? comment,
        CancellationToken cancellationToken = default);

    Task<CopilotActionProposal> RejectAsync(
        Guid proposalId,
        string? comment,
        CancellationToken cancellationToken = default);

    Task<CopilotActionProposal?> GetAsync(
        Guid proposalId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CopilotActionProposal>> GetPendingAsync(
        CancellationToken cancellationToken = default);
}
