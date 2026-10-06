namespace EmergencyResponse.Application.Knowledge;

public interface IKnowledgeDocumentChunker
{
    IReadOnlyList<KnowledgeChunk> Chunk(
        IReadOnlyList<KnowledgeTextPage> pages);
}
