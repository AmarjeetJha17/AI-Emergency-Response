using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Domain.Copilot;

namespace EmergencyResponse.UnitTests.Copilot.Actions;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class CopilotActionStateMachineTests
{
    [Theory]
    [InlineData(CopilotActionStatus.Proposed, CopilotActionStatus.AwaitingApproval)]
    [InlineData(CopilotActionStatus.AwaitingApproval, CopilotActionStatus.Approved)]
    [InlineData(CopilotActionStatus.AwaitingApproval, CopilotActionStatus.Rejected)]
    [InlineData(CopilotActionStatus.Approved, CopilotActionStatus.Executing)]
    [InlineData(CopilotActionStatus.Executing, CopilotActionStatus.Succeeded)]
    public void Allowed_transitions_are_explicit(
        CopilotActionStatus current,
        CopilotActionStatus next)
    {
        Assert.True(CopilotActionStateMachine.CanTransition(current, next));
    }

    [Theory]
    [InlineData(CopilotActionStatus.Succeeded, CopilotActionStatus.Approved)]
    [InlineData(CopilotActionStatus.Rejected, CopilotActionStatus.Executing)]
    [InlineData(CopilotActionStatus.Expired, CopilotActionStatus.Approved)]
    public void Invalid_transitions_are_rejected(
        CopilotActionStatus current,
        CopilotActionStatus next)
    {
        Assert.Throws<InvalidOperationException>(() =>
            CopilotActionStateMachine.EnsureTransition(current, next));
    }
}
