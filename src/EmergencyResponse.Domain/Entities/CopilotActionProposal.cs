using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Copilot;

namespace EmergencyResponse.Domain.Entities;

public sealed class CopilotActionProposal : BaseEntity
{
    public Guid CopilotInteractionId { get; set; }

    public CopilotActionType ActionType { get; set; }

    public CopilotActionRiskLevel RiskLevel { get; set; }

    public CopilotActionStatus Status { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ArgumentsJson { get; set; } = "{}";

    public Guid? IncidentId { get; set; }

    public Guid? SituationId { get; set; }

    public Guid? ResourceId { get; set; }

    public Guid? EscalationId { get; set; }

    public string ProposedByUserId { get; set; } = string.Empty;

    public DateTime ProposedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public CopilotApprovalDecision ApprovalDecision { get; set; }

    public string? ApprovedByUserId { get; set; }

    public DateTime? ApprovedAtUtc { get; set; }

    public string? ApprovalComment { get; set; }

    public DateTime? ExecutedAtUtc { get; set; }

    public string? ExecutionResultJson { get; set; }

    public string? FailureReason { get; set; }

    public string? PolicySnapshotJson { get; set; }

    public string CorrelationId { get; set; } = string.Empty;
}
