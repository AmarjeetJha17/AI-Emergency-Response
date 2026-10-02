namespace EmergencyResponse.Application.Predictive.Hotspots;

public sealed class HotspotOptions
{
    public double CellSizeDegrees { get; init; } = 0.01;

    public int RecentHours { get; init; } = 6;

    public int HistoricalHours { get; init; } = 168;

    public double MinimumActivity { get; init; } = 3;
}
