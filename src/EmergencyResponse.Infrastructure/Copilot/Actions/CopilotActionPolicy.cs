using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Domain.Copilot;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Copilot.Actions;

public sealed class CopilotActionPolicy : ICopilotActionPolicy
{
    private readonly CopilotActionOptions _options;

    public CopilotActionPolicy(IOptions<CopilotActionOptions> options)
    {
        _options = options.Value;
    }

    public CopilotActionPolicyDecision Evaluate(
        CopilotActionType actionType,
        IReadOnlyCollection<string> roles,
        DateTime nowUtc)
    {
        var requiredRoles = actionType switch
        {
            CopilotActionType.CreateIncidentNote or
            CopilotActionType.AddIncidentTag =>
                new[] { ApplicationRoles.Operator, ApplicationRoles.EmergencyAdmin, ApplicationRoles.SystemAdmin },
            _ => Array.Empty<string>()
        };

        var allowed = requiredRoles.Any(roles.Contains);
        var enabled = actionType is
            CopilotActionType.CreateIncidentNote or
            CopilotActionType.AddIncidentTag;

        return new CopilotActionPolicyDecision
        {
            Allowed = enabled && allowed,
            RequiresApproval = enabled && allowed,
            RequiredRoles = requiredRoles,
            RiskLevel = CopilotActionRiskLevel.Low,
            ExpiresAtUtc = nowUtc.AddMinutes(
                Math.Clamp(_options.ProposalExpirationMinutes, 1, 30)),
            DenialReason = !enabled
                ? "This action type is not enabled for Copilot."
                : !allowed
                    ? "The current user is not authorized for this action."
                    : null
        };
    }
}
