using EmergencyResponse.Domain.Copilot;

namespace EmergencyResponse.Application.Copilot.Actions;

public sealed class CopilotActionPolicyDecision
{
    public bool Allowed { get; init; }

    public bool RequiresApproval { get; init; }

    public IReadOnlyList<string> RequiredRoles { get; init; } = [];

    public CopilotActionRiskLevel RiskLevel { get; init; }

    public DateTime ExpiresAtUtc { get; init; }

    public string? DenialReason { get; init; }

    public string PolicyVersion { get; init; } = "CopilotActionPolicy_v1";
}

public interface ICopilotActionPolicy
{
    CopilotActionPolicyDecision Evaluate(
        CopilotActionType actionType,
        IReadOnlyCollection<string> roles,
        DateTime nowUtc);
}
