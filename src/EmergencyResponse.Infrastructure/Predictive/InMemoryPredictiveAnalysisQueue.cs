using System.Threading.Channels;
using EmergencyResponse.Application.Observability;
using EmergencyResponse.Application.Predictive.Interfaces;
using EmergencyResponse.Application.Predictive.Jobs;

namespace EmergencyResponse.Infrastructure.Predictive;

public sealed class InMemoryPredictiveAnalysisQueue : IPredictiveAnalysisQueue
{
    private readonly Channel<PredictiveAnalysisJob> _channel;

    public InMemoryPredictiveAnalysisQueue()
    {
        _channel = Channel.CreateBounded<PredictiveAnalysisJob>(
            new BoundedChannelOptions(1000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
    }

    public async ValueTask EnqueueAsync(
        PredictiveAnalysisJob job,
        CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(job, cancellationToken);
        EmergencyResponseTelemetry.PredictionJobsEnqueued.Add(1);
        EmergencyResponseTelemetry.PredictionQueueLength.Add(1);
    }

    public async ValueTask<PredictiveAnalysisJob> DequeueAsync(
        CancellationToken cancellationToken)
    {
        var job = await _channel.Reader.ReadAsync(cancellationToken);
        EmergencyResponseTelemetry.PredictionJobsProcessed.Add(1);
        EmergencyResponseTelemetry.PredictionQueueLength.Add(-1);
        return job;
    }
}
