namespace EmergencyResponse.Application.Dashboard;

public sealed record DashboardTimeSeriesPoint
{
    public DateTime Date { get; init; }

    public int Count { get; init; }
}
