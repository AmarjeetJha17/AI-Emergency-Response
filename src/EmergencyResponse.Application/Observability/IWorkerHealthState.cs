namespace EmergencyResponse.Application.Observability;

using EmergencyResponse.Application.Reliability;

public interface IWorkerHealthState
{
    void MarkStarted(string workerName);
    void MarkSuccess();
    void MarkSuccess(string workerName);
    DateTime? LastSuccessUtc { get; }
    DateTime? GetLastSuccessUtc(string workerName);
    void MarkRunning(string workerName);
    void MarkDraining(string workerName);
    void MarkStopped(string workerName);
    void MarkFaulted(string workerName, string failureCategory);
    WorkerHealthSnapshot? GetSnapshot(string workerName);
    IReadOnlyList<WorkerHealthSnapshot> GetSnapshots();
}
