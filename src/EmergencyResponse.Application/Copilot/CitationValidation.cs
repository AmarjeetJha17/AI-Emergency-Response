using EmergencyResponse.Application.Knowledge;

namespace EmergencyResponse.Application.Copilot;

public interface ICitationValidator
{
    Task<CitationValidationResult> ValidateAsync(
        IReadOnlyList<CopilotCitation> citations,
        IReadOnlyList<KnowledgeSearchResult> retrievedResults,
        CancellationToken cancellationToken = default);
}

public sealed class CitationValidationResult
{
    public bool IsValid { get; init; }

    public IReadOnlyList<CopilotCitation> ValidCitations { get; init; } = [];

    public IReadOnlyList<CopilotCitation> InvalidCitations { get; init; } = [];
}

public interface ICopilotPolicyValidator
{
    CopilotPolicyValidationResult Validate(
        EmergencyCopilotStructuredResponse response,
        IReadOnlyList<CopilotCitation> validCitations);
}

public sealed class CopilotPolicyValidationResult
{
    public bool Allowed { get; init; }

    public IReadOnlyList<string> Violations { get; init; } = [];
}
