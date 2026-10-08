namespace EmergencyResponse.Application.Reliability;

public sealed class AIServiceCircuitOpenException(string message) : InvalidOperationException(message);

public interface IAICircuitBreaker
{
    AIServiceHealthSnapshot Snapshot { get; }
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
}
