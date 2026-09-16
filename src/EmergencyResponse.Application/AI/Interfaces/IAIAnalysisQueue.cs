using EmergencyResponse.Application.AI.Jobs;

namespace EmergencyResponse.Application.AI.Interfaces;

public interface IAIAnalysisQueue
{
    ValueTask EnqueueAsync(
        IncidentAIJob job,
        CancellationToken cancellationToken = default);

    ValueTask<IncidentAIJob> DequeueAsync(
        CancellationToken cancellationToken);
}