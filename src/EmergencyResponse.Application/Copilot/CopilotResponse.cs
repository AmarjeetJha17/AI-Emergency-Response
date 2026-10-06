using EmergencyResponse.Domain.Knowledge;

namespace EmergencyResponse.Application.Copilot;

public sealed class CopilotResponse
{
    public Guid? ConversationId { get; init; }

    public string Answer { get; init; } = string.Empty;

    public IReadOnlyList<CopilotCitation> Citations { get; init; } = [];

    public IReadOnlyList<CopilotClaim> Claims { get; init; } = [];

    public IReadOnlyList<string> VerificationNotes { get; init; } = [];

    public double? GroundingConfidence { get; init; }

    public GroundingStatus GroundingStatus { get; init; }

    public bool SourcesSufficient { get; init; }

    public string SourcesInsufficiencyReason { get; init; } = string.Empty;

    public bool RequiresHumanVerification { get; init; }

    public CopilotFailureReason? FailureReason { get; init; }

    public IReadOnlyList<Actions.CopilotActionProposalResult> ActionProposals { get; init; } = [];
}
