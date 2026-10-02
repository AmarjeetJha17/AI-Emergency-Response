namespace EmergencyResponse.Application.ExternalContext;

public sealed class AzureMapsOptions
{
    public const string SectionName = "AzureMaps";

    public string Endpoint { get; set; } = "https://atlas.microsoft.com";

    public string ClientId { get; set; } = string.Empty;

    public bool Enabled { get; set; }

    public int WeatherCacheMinutes { get; set; } = 10;

    public int SevereAlertCacheMinutes { get; set; } = 5;

    public int TrafficCacheMinutes { get; set; } = 5;
}
