namespace EmergencyResponse.Application.Predictive.Hotspots;

public sealed class GeoGridCell
{
    public string CellKey { get; init; } = string.Empty;

    public double CenterLatitude { get; init; }

    public double CenterLongitude { get; init; }
}
