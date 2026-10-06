using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Domain.Copilot;
using EmergencyResponse.Infrastructure.Copilot.Actions;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.UnitTests.Copilot.Actions;

public sealed class CopilotActionPolicyTests
{
    private readonly ICopilotActionPolicy _policy =
        new CopilotActionPolicy(
            Options.Create(new CopilotActionOptions
            {
                ProposalExpirationMinutes = 5
            }));

    [Fact]
    public void Operator_can_propose_enabled_note_action()
    {
        var decision = _policy.Evaluate(
            CopilotActionType.CreateIncidentNote,
            [ApplicationRoles.Operator],
            DateTime.UtcNow);

        Assert.True(decision.Allowed);
        Assert.True(decision.RequiresApproval);
        Assert.Equal(CopilotActionRiskLevel.Low, decision.RiskLevel);
    }

    [Fact]
    public void Disabled_dispatch_action_is_denied()
    {
        var decision = _policy.Evaluate(
            CopilotActionType.DispatchResource,
            [ApplicationRoles.EmergencyAdmin],
            DateTime.UtcNow);

        Assert.False(decision.Allowed);
        Assert.Contains("not enabled", decision.DenialReason);
    }

    [Fact]
    public void Citizen_role_is_denied_for_note_action()
    {
        var decision = _policy.Evaluate(
            CopilotActionType.AddIncidentTag,
            ["Citizen"],
            DateTime.UtcNow);

        Assert.False(decision.Allowed);
    }
}
