namespace EmergencyResponse.Application.Knowledge;

public sealed class KnowledgeDocumentOptions
{
    public const string SectionName = "Knowledge";

    public string StorageRootPath { get; init; } = "private-knowledge";

    public long MaxFileSizeBytes { get; init; } = 25 * 1024 * 1024;

    public int MaximumChunkCharacters { get; init; } = 3500;

    public int ChunkOverlapCharacters { get; init; } = 250;
}
