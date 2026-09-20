namespace EmergencyResponse.Application.Common.Configuration;

public sealed class MapOptions
{
    public const string SectionName = "Map";

    public string TileUrl { get; set; } = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";

    public string Attribution { get; set; } = "&copy; <a href=\"https://www.openstreetmap.org/copyright\">OpenStreetMap</a> contributors";

    public double DefaultLatitude { get; set; } = 20.5937;

    public double DefaultLongitude { get; set; } = 78.9629;

    public int DefaultZoom { get; set; } = 5;
}
