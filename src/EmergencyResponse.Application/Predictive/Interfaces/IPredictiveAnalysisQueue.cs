using EmergencyResponse.Application.Predictive.Jobs;

namespace EmergencyResponse.Application.Predictive.Interfaces;

public interface IPredictiveAnalysisQueue
{
    ValueTask EnqueueAsync(
        PredictiveAnalysisJob job,
        CancellationToken cancellationToken = default);

    ValueTask<PredictiveAnalysisJob> DequeueAsync(
        CancellationToken cancellationToken);
}
