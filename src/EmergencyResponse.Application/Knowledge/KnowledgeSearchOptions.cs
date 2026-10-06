namespace EmergencyResponse.Application.Knowledge;

public sealed class KnowledgeSearchOptions
{
    public const string SectionName = "KnowledgeSearch";

    public string Endpoint { get; init; } = string.Empty;

    public string IndexName { get; init; } = "emergency-knowledge";

    public int TopK { get; init; } = 8;

    public double MinimumRetrievalScore { get; init; } = 0.55;

    public int MaximumContextChunks { get; init; } = 6;
}
