namespace EmergencyResponse.Application.Copilot;

public sealed class CopilotCitation
{
    public string SearchDocumentId { get; init; } = string.Empty;

    public string DocumentNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public int PageNumber { get; init; }

    public string? SectionHeading { get; init; }

    public string CitationText { get; init; } = string.Empty;
}
