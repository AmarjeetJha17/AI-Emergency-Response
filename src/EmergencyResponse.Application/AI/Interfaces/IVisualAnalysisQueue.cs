using EmergencyResponse.Application.AI.Jobs;

namespace EmergencyResponse.Application.AI.Interfaces;

public interface IVisualAnalysisQueue
{
    ValueTask EnqueueAsync(
        VisualAnalysisJob job,
        CancellationToken cancellationToken = default);

    ValueTask<VisualAnalysisJob> DequeueAsync(
        CancellationToken cancellationToken);
}
