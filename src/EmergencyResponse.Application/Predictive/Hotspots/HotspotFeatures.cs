namespace EmergencyResponse.Application.Predictive.Hotspots;

public sealed class HotspotFeatures
{
    public string CellKey { get; init; } = string.Empty;

    public int RecentIncidentCount { get; init; }

    public int HistoricalIncidentCount { get; init; }

    public int RecentCriticalCount { get; init; }

    public int RecentHighCount { get; init; }

    public int PeopleAffected { get; init; }

    public double GrowthRatio { get; init; }
}
