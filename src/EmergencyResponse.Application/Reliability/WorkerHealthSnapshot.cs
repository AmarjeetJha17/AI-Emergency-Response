namespace EmergencyResponse.Application.Reliability;

public sealed record WorkerHealthSnapshot(
    string WorkerName,
    WorkerLifecycleState State,
    DateTimeOffset StateChangedAtUtc,
    DateTimeOffset? LastHeartbeatAtUtc,
    string? LastFailureCategory);
