using System.Collections.Concurrent;
using EmergencyResponse.Application.Reliability;

namespace EmergencyResponse.Application.Observability;

public sealed class WorkerHealthState : IWorkerHealthState
{
    private long _lastSuccessTicks;
    private readonly ConcurrentDictionary<string, long> _workerTicks = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, WorkerHealthSnapshot> _workers = new(StringComparer.OrdinalIgnoreCase);

    public void MarkSuccess()
    {
        Interlocked.Exchange(ref _lastSuccessTicks, DateTime.UtcNow.Ticks);
    }

    public void MarkSuccess(string workerName)
    {
        var nowTicks = DateTime.UtcNow.Ticks;
        Interlocked.Exchange(ref _lastSuccessTicks, nowTicks);
        _workerTicks.AddOrUpdate(workerName, _ => nowTicks, (_, _) => nowTicks);
        _workers.AddOrUpdate(
            workerName,
            name => new WorkerHealthSnapshot(name, WorkerLifecycleState.Running, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null),
            (name, previous) => previous with
            {
                State = WorkerLifecycleState.Running,
                LastHeartbeatAtUtc = DateTimeOffset.UtcNow,
                LastFailureCategory = null
            });
    }

    public void MarkStarted(string workerName)
    {
        MarkRunning(workerName);
    }

    public void MarkRunning(string workerName) => Transition(workerName, WorkerLifecycleState.Running, null);
    public void MarkDraining(string workerName) => Transition(workerName, WorkerLifecycleState.Draining, null);
    public void MarkStopped(string workerName) => Transition(workerName, WorkerLifecycleState.Stopped, null);
    public void MarkFaulted(string workerName, string failureCategory)
        => Transition(workerName, WorkerLifecycleState.Faulted, failureCategory);

    public WorkerHealthSnapshot? GetSnapshot(string workerName)
        => _workers.TryGetValue(workerName, out var snapshot) ? snapshot : null;

    public IReadOnlyList<WorkerHealthSnapshot> GetSnapshots()
        => _workers.Values.OrderBy(x => x.WorkerName, StringComparer.OrdinalIgnoreCase).ToArray();

    private void Transition(string workerName, WorkerLifecycleState state, string? failureCategory)
    {
        var now = DateTimeOffset.UtcNow;
        _workers.AddOrUpdate(
            workerName,
            name => new WorkerHealthSnapshot(name, state, now, null, failureCategory),
            (_, previous) => previous with
            {
                State = state,
                StateChangedAtUtc = now,
                LastFailureCategory = failureCategory
            });
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
