namespace EmergencyResponse.Application.Knowledge;

public sealed record KnowledgeDocumentJob(Guid DocumentId);

public interface IKnowledgeDocumentQueue
{
    ValueTask EnqueueAsync(
        KnowledgeDocumentJob job,
        CancellationToken cancellationToken = default);

    ValueTask<KnowledgeDocumentJob> DequeueAsync(
        CancellationToken cancellationToken = default);
}
