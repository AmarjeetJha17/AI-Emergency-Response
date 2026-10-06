using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.Knowledge;

namespace EmergencyResponse.Infrastructure.Copilot;

public sealed class CitationValidator : ICitationValidator
{
    public Task<CitationValidationResult> ValidateAsync(
        IReadOnlyList<CopilotCitation> citations,
        IReadOnlyList<KnowledgeSearchResult> retrievedResults,
        CancellationToken cancellationToken = default)
    {
        var valid = citations
            .Where(citation => retrievedResults.Any(result =>
                (!string.IsNullOrWhiteSpace(citation.SearchDocumentId) &&
                 result.SearchDocumentId == citation.SearchDocumentId) ||
                (result.DocumentNumber == citation.DocumentNumber &&
                 result.Title == citation.Title &&
                 result.Version == citation.Version &&
                 result.PageNumber == citation.PageNumber)))
            .ToList();
        var invalid = citations.Except(valid).ToList();
        return Task.FromResult(new CitationValidationResult
        {
            IsValid = invalid.Count == 0,
            ValidCitations = valid,
            InvalidCitations = invalid
        });
    }
}
