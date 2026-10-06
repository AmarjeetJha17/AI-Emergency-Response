namespace EmergencyResponse.Application.Knowledge;

public sealed class KnowledgeSearchResult
{
    public string SearchDocumentId { get; init; } = string.Empty;

    public string DocumentNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Version { get; init; } = string.Empty;

    public int PageNumber { get; init; }

    public string? SectionHeading { get; init; }

    public string Content { get; init; } = string.Empty;

    public double? Score { get; init; }

    public string DocumentType { get; init; } = string.Empty;
}
