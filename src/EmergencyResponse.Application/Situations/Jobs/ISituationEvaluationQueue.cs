namespace EmergencyResponse.Application.Situations.Jobs;

public interface ISituationEvaluationQueue
{
    ValueTask EnqueueAsync(
        SituationEvaluationJob job,
        CancellationToken cancellationToken = default);

    ValueTask<SituationEvaluationJob> DequeueAsync(
        CancellationToken cancellationToken);
}
