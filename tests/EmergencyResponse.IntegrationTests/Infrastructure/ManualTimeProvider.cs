namespace EmergencyResponse.IntegrationTests.Infrastructure;

/// <summary>A deterministic UTC clock for integration tests involving expiry.</summary>
public sealed class ManualTimeProvider : TimeProvider
{
    private readonly object _gate = new();
    private DateTimeOffset _utcNow;

    public ManualTimeProvider(DateTimeOffset? initialUtcNow = null)
    {
        _utcNow = initialUtcNow ?? new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    }

    public override DateTimeOffset GetUtcNow()
    {
        lock (_gate)
        {
            return _utcNow;
        }
    }

    public void SetUtcNow(DateTimeOffset utcNow)
    {
        lock (_gate)
        {
            _utcNow = utcNow.ToUniversalTime();
        }
    }

    public void Advance(TimeSpan duration)
    {
        lock (_gate)
        {
            _utcNow += duration;
        }
    }
}
