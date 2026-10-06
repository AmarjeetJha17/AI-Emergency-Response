using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.Copilot.Actions;

namespace EmergencyResponse.Web.Services;

public interface ICopilotApiClient
{
    Task<CopilotResponse?> QueryAsync(
        CopilotQuery query,
        CancellationToken cancellationToken = default);

    Task<CopilotActionExecutionResult> ApproveActionAsync(
        Guid proposalId,
        string? comment,
        CancellationToken cancellationToken = default);

    Task<CopilotActionProposalResult?> RejectActionAsync(
        Guid proposalId,
        string? comment,
        CancellationToken cancellationToken = default);
}
