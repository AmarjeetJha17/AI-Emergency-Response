using EmergencyResponse.Application.Observability;
using EmergencyResponse.Application.Reliability;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class AICircuitBreaker : IAICircuitBreaker
{
    private readonly object _gate = new();
    private readonly AICircuitBreakerOptions _options;
    private readonly AIServiceHealthState _health;
    private DateTimeOffset? _openedAt;
    private bool _probeActive;

    public AICircuitBreaker(
        IOptions<AICircuitBreakerOptions> options,
        AIServiceHealthState health)
    {
        _options = options.Value;
        _health = health;
    }

    public AIServiceHealthSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                if (_openedAt is not null && DateTimeOffset.UtcNow - _openedAt < TimeSpan.FromSeconds(_options.OpenDurationSeconds))
                {
                    return _health.Snapshot with { State = DependencyHealthState.Unavailable };
                }
            }
            return _health.Snapshot;
        }
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var isProbe = false;
        lock (_gate)
        {
            if (_openedAt is not null)
            {
                if (DateTimeOffset.UtcNow - _openedAt < TimeSpan.FromSeconds(_options.OpenDurationSeconds))
                {
                    throw new AIServiceCircuitOpenException("AI service circuit is open.");
                }
                if (_probeActive)
                {
                    throw new AIServiceCircuitOpenException("AI service circuit is awaiting its half-open probe.");
                }
                _probeActive = true;
                isProbe = true;
            }
        }

        try
        {
            var result = await operation(cancellationToken);
            _health.MarkSuccess();
            lock (_gate)
            {
                _openedAt = null;
                _probeActive = false;
            }
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            if (isProbe)
            {
                lock (_gate) _probeActive = false;
            }
            throw;
        }
        catch (Exception exception)
        {
            var category = ReliabilityFailureClassifier.Categorize(exception);
            _health.MarkFailure(category);
            var open = false;
            lock (_gate)
            {
                _probeActive = false;
                if (isProbe || _health.Snapshot.ConsecutiveFailures >= _options.FailureThreshold)
                {
                    _openedAt = DateTimeOffset.UtcNow;
                    _health.MarkUnavailable(category);
                    open = true;
                }
            }
            if (open)
            {
                EmergencyResponseTelemetry.DependencyCircuitOpen.Add(1, new KeyValuePair<string, object?>("dependency", "ai"));
            }
            throw;
        }
    }
}
