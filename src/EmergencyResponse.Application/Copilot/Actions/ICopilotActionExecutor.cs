using EmergencyResponse.Domain.Entities;

namespace EmergencyResponse.Application.Copilot.Actions;

public interface ICopilotActionExecutor
{
    Task<CopilotActionExecutionResult> ExecuteAsync(
        Guid proposalId,
        CancellationToken cancellationToken = default);
}
