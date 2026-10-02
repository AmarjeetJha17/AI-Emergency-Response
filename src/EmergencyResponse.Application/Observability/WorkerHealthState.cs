using System.Collections.Concurrent;

namespace EmergencyResponse.Application.Observability;

public sealed class WorkerHealthState : IWorkerHealthState
{
    private long _lastSuccessTicks;
    private readonly ConcurrentDictionary<string, long> _workerTicks = new(StringComparer.OrdinalIgnoreCase);

    public void MarkSuccess()
    {
        Interlocked.Exchange(ref _lastSuccessTicks, DateTime.UtcNow.Ticks);
    }

    public void MarkSuccess(string workerName)
    {
        var nowTicks = DateTime.UtcNow.Ticks;
        Interlocked.Exchange(ref _lastSuccessTicks, nowTicks);
        _workerTicks.AddOrUpdate(workerName, _ => nowTicks, (_, _) => nowTicks);
    }

    public void MarkStarted(string workerName)
    {
        MarkSuccess(workerName);
    }

    public DateTime? LastSuccessUtc
    {
        get
        {
            var ticks = Interlocked.Read(ref _lastSuccessTicks);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }
    }

    public DateTime? GetLastSuccessUtc(string workerName)
    {
        if (_workerTicks.TryGetValue(workerName, out var ticks) && ticks > 0)
        {
            return new DateTime(ticks, DateTimeKind.Utc);
        }
        return LastSuccessUtc;
    }
}
