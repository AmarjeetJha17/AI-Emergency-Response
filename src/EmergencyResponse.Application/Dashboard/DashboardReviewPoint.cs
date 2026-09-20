namespace EmergencyResponse.Application.Dashboard;

public sealed record DashboardReviewPoint
{
    public string Category { get; init; } = null!;

    public int Count { get; init; }
}
