namespace EmergencyResponse.Application.Predictive.Hotspots;

public sealed class HotspotResult
{
    public string CellKey { get; init; } = string.Empty;

    public double CenterLatitude { get; init; }

    public double CenterLongitude { get; init; }

    public double HotspotScore { get; init; }

    public int RecentIncidentCount { get; init; }

    public int ForecastedIncidentCount { get; init; }

    public int CriticalIncidentCount { get; init; }

    public int HighIncidentCount { get; init; }

    public double GrowthRatio { get; init; }

    public string Trend { get; init; } = string.Empty;
}
