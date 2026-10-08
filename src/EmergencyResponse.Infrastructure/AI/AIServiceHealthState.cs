using EmergencyResponse.Application.Reliability;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class AIServiceHealthState : IAIServiceHealthState
{
    private readonly object _gate = new();
    private int _failures;
    private DateTimeOffset? _lastSuccess;
    private DateTimeOffset? _lastFailure;
    private string? _category;
    private DependencyHealthState _state = DependencyHealthState.Healthy;

    public AIServiceHealthSnapshot Snapshot
    {
        get
        {
            lock (_gate)
            {
                return new AIServiceHealthSnapshot
                {
                    State = _state,
                    ConsecutiveFailures = _failures,
                    LastSuccessAtUtc = _lastSuccess,
                    LastFailureAtUtc = _lastFailure,
                    LastFailureCategory = _category
                };
            }
        }
    }

    public void MarkSuccess()
    {
        lock (_gate)
        {
            _failures = 0;
            _lastSuccess = DateTimeOffset.UtcNow;
            _category = null;
            _state = DependencyHealthState.Healthy;
        }
    }

    public void MarkFailure(string category)
    {
        lock (_gate)
        {
            _failures++;
            _lastFailure = DateTimeOffset.UtcNow;
            _category = category;
            _state = DependencyHealthState.Degraded;
        }
    }

    internal void MarkUnavailable(string category)
    {
        lock (_gate)
        {
            _lastFailure = DateTimeOffset.UtcNow;
            _category = category;
            _state = DependencyHealthState.Unavailable;
        }
    }
}
