using System.Threading.Channels;

using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;
using EmergencyResponse.Application.Observability;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class InMemoryAIAnalysisQueue
    : IAIAnalysisQueue
{
    private readonly Channel<IncidentAIJob> _channel;

    public InMemoryAIAnalysisQueue()
    {
        _channel =
            Channel.CreateBounded<IncidentAIJob>(
                new BoundedChannelOptions(1000)
                {
                    FullMode =
                        BoundedChannelFullMode.Wait,

                    SingleReader = true,
                    SingleWriter = false
                });
    }

    public async ValueTask EnqueueAsync(
        IncidentAIJob job,
        CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(
            job,
            cancellationToken);

        EmergencyResponseTelemetry.AIJobsEnqueued.Add(1);
        EmergencyResponseTelemetry.AIQueueLength.Add(1);
    }

    public async ValueTask<IncidentAIJob> DequeueAsync(
        CancellationToken cancellationToken)
    {
        var job = await _channel.Reader.ReadAsync(
            cancellationToken);

        EmergencyResponseTelemetry.AIJobsProcessed.Add(1);
        EmergencyResponseTelemetry.AIQueueLength.Add(-1);

        return job;
    }
}