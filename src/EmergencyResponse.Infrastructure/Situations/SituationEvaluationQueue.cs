using System.Threading.Channels;
using EmergencyResponse.Application.Situations.Jobs;

namespace EmergencyResponse.Infrastructure.Situations;

public sealed class SituationEvaluationQueue : ISituationEvaluationQueue
{
    private readonly Channel<SituationEvaluationJob> _channel;

    public SituationEvaluationQueue()
    {
        _channel = Channel.CreateBounded<SituationEvaluationJob>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
    }

    public async ValueTask EnqueueAsync(
        SituationEvaluationJob job,
        CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(job, cancellationToken);
    }

    public async ValueTask<SituationEvaluationJob> DequeueAsync(
        CancellationToken cancellationToken)
    {
        return await _channel.Reader.ReadAsync(cancellationToken);
    }
}
