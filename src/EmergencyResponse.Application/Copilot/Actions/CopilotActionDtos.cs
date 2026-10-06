using EmergencyResponse.Domain.Copilot;

namespace EmergencyResponse.Application.Copilot.Actions;

public sealed record ApproveCopilotActionRequest(string? Comment);

public sealed record RejectCopilotActionRequest(string? Comment);

public sealed record CopilotActionProposalResult(
    Guid ProposalId,
    CopilotActionType ActionType,
    CopilotActionStatus Status,
    CopilotActionRiskLevel RiskLevel,
    bool RequiresApproval,
    string Summary,
    DateTime? ExpiresAtUtc);

public sealed record CopilotActionExecutionResult(
    bool Succeeded,
    string Message,
    string? ErrorCode,
    string? ResultJson,
    DateTime ExecutedAtUtc);

public sealed record CopilotActionProposalView(
    Guid Id,
    CopilotActionType ActionType,
    CopilotActionRiskLevel RiskLevel,
    CopilotActionStatus Status,
    string Title,
    string Description,
    Guid? IncidentId,
    DateTime ProposedAtUtc,
    DateTime? ExpiresAtUtc,
    CopilotApprovalDecision ApprovalDecision,
    string? ApprovedByUserId,
    DateTime? ApprovedAtUtc,
    string? ApprovalComment,
    DateTime? ExecutedAtUtc,
    string? FailureReason,
    string CorrelationId);
