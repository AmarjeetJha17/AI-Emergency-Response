using EmergencyResponse.Domain.Copilot;

namespace EmergencyResponse.Application.Copilot.Actions;

public static class CopilotActionStateMachine
{
    public static bool CanTransition(
        CopilotActionStatus current,
        CopilotActionStatus next) =>
        (current, next) switch
        {
            (CopilotActionStatus.Proposed, CopilotActionStatus.AwaitingApproval) => true,
            (CopilotActionStatus.AwaitingApproval, CopilotActionStatus.Approved) => true,
            (CopilotActionStatus.AwaitingApproval, CopilotActionStatus.Rejected) => true,
            (CopilotActionStatus.AwaitingApproval, CopilotActionStatus.Expired) => true,
            (CopilotActionStatus.Approved, CopilotActionStatus.Executing) => true,
            (CopilotActionStatus.Executing, CopilotActionStatus.Succeeded) => true,
            (CopilotActionStatus.Executing, CopilotActionStatus.Failed) => true,
            _ => false
        };

    public static void EnsureTransition(
        CopilotActionStatus current,
        CopilotActionStatus next)
    {
        if (!CanTransition(current, next))
        {
            throw new InvalidOperationException(
                $"Action cannot transition from {current} to {next}.");
        }
    }
}
