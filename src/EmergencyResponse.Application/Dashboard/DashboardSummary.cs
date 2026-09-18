namespace EmergencyResponse.Application.Dashboard;

public sealed record DashboardSummary
{
    public int CriticalIncidents { get; init; }

    public int HighIncidents { get; init; }

    public int ModerateIncidents { get; init; }

    public int LowIncidents { get; init; }

    public int ActiveIncidents { get; init; }

    public int UnassignedIncidents { get; init; }

    public int AwaitingAIReview { get; init; }

    public int AwaitingDuplicateReview { get; init; }

    public int AwaitingVisualReview { get; init; }
}
