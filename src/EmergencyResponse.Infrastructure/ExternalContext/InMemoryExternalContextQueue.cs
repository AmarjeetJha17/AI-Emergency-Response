using System.Threading.Channels;
using EmergencyResponse.Application.ExternalContext;

namespace EmergencyResponse.Infrastructure.ExternalContext;

public sealed class InMemoryExternalContextQueue : IExternalContextQueue
{
    private readonly Channel<ExternalContextJob> _channel =
        Channel.CreateBounded<ExternalContextJob>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

    public ValueTask EnqueueAsync(
        ExternalContextJob job,
        CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(job, cancellationToken);

    public ValueTask<ExternalContextJob> DequeueAsync(
        CancellationToken cancellationToken = default)
        => _channel.Reader.ReadAsync(cancellationToken);
}
