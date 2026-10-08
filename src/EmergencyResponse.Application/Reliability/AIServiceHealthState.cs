namespace EmergencyResponse.Application.Reliability;

public interface IAIServiceHealthState
{
    AIServiceHealthSnapshot Snapshot { get; }
    void MarkSuccess();
    void MarkFailure(string category);
}
