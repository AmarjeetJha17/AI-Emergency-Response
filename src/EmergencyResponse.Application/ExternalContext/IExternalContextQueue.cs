namespace EmergencyResponse.Application.ExternalContext;

public interface IExternalContextQueue
{
    ValueTask EnqueueAsync(
        ExternalContextJob job,
        CancellationToken cancellationToken = default);

    ValueTask<ExternalContextJob> DequeueAsync(
        CancellationToken cancellationToken = default);
}
