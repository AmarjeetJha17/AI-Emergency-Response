namespace EmergencyResponse.Application.Dashboard;

public sealed record DashboardChartPoint
{
    public string Label { get; init; } = null!;

    public int Value { get; init; }
}
