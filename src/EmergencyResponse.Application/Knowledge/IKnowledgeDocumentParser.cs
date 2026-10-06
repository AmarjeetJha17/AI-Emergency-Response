namespace EmergencyResponse.Application.Knowledge;

public interface IKnowledgeDocumentParser
{
    Task<IReadOnlyList<KnowledgeTextPage>> ParseAsync(
        Stream content,
        string fileName,
        CancellationToken cancellationToken = default);
}
