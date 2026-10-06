using System.Threading.Channels;
using EmergencyResponse.Application.Knowledge;

namespace EmergencyResponse.Infrastructure.Knowledge;

public sealed class InMemoryKnowledgeDocumentQueue
    : IKnowledgeDocumentQueue
{
    private readonly Channel<KnowledgeDocumentJob> _channel =
        Channel.CreateUnbounded<KnowledgeDocumentJob>();

    public ValueTask EnqueueAsync(
        KnowledgeDocumentJob job,
        CancellationToken cancellationToken = default) =>
        _channel.Writer.WriteAsync(job, cancellationToken);

    public ValueTask<KnowledgeDocumentJob> DequeueAsync(
        CancellationToken cancellationToken = default) =>
        _channel.Reader.ReadAsync(cancellationToken);
}
