namespace EmergencyResponse.Application.Copilot;

public sealed class EmergencyCopilotStructuredResponse
{
    public string Answer { get; init; } = string.Empty;

    public IReadOnlyList<CopilotCitation> Citations { get; init; } = [];

    public IReadOnlyList<CopilotClaim> Claims { get; init; } = [];

    public IReadOnlyList<string> VerificationNotes { get; init; } = [];

    public bool SourcesSufficient { get; init; }

    public bool RequiresHumanVerification { get; init; }

    public string SourcesInsufficiencyReason { get; init; } = string.Empty;

    public IReadOnlyList<Actions.CopilotActionProposalResult> ActionProposals { get; init; } = [];
}
