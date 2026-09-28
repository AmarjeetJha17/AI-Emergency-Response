namespace EmergencyResponse.Application.Situations.Jobs;

public sealed record SituationEvaluationJob
{
    public Guid? TriggerIncidentId { get; init; }

    public string? TriggerReason { get; init; }

    public SituationEvaluationJob()
    {
    }

    public SituationEvaluationJob(Guid? triggerIncidentId, string? triggerReason = null)
    {
        TriggerIncidentId = triggerIncidentId;
        TriggerReason = triggerReason;
    }
}
