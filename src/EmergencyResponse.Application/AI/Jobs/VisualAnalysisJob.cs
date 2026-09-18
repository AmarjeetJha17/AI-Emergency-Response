namespace EmergencyResponse.Application.AI.Jobs;

public sealed record VisualAnalysisJob
{
    public Guid IncidentMediaId { get; init; }

    public bool IsVideo { get; init; }
}
