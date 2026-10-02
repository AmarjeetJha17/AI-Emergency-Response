namespace EmergencyResponse.Application.Observability;

public interface IWorkerHealthState
{
    void MarkStarted(string workerName);
    void MarkSuccess();
    void MarkSuccess(string workerName);
    DateTime? LastSuccessUtc { get; }
    DateTime? GetLastSuccessUtc(string workerName);
}
