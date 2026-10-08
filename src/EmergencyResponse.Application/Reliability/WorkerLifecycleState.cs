namespace EmergencyResponse.Application.Reliability;

public enum WorkerLifecycleState
{
    Starting = 0,
    Running = 1,
    Draining = 2,
    Stopped = 3,
    Faulted = 4
}
