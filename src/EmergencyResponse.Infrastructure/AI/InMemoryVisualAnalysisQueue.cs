using System.Threading.Channels;

using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class InMemoryVisualAnalysisQueue : IVisualAnalysisQueue
{
    private readonly Channel<VisualAnalysisJob> _channel;

    public InMemoryVisualAnalysisQueue()
    {
        _channel = Channel.CreateBounded<VisualAnalysisJob>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
    }

    public async ValueTask EnqueueAsync(
        VisualAnalysisJob job,
        CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public async ValueTask<VisualAnalysisJob> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
