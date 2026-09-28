namespace EmergencyResponse.Application.Observability;

public interface IWorkerHealthState
{
    void MarkSuccess();
    void MarkSuccess(string workerName);
    DateTime? LastSuccessUtc { get; }
    DateTime? GetLastSuccessUtc(string workerName);
}
