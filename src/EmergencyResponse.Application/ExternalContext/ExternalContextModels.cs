namespace EmergencyResponse.Application.ExternalContext;

public sealed class WeatherContextData
{
    public DateTime ObservedAtUtc { get; init; }
    public double? TemperatureC { get; init; }
    public double? ApparentTemperatureC { get; init; }
    public double? RelativeHumidity { get; init; }
    public double? WindSpeedKph { get; init; }
    public double? WindDirectionDegrees { get; init; }
    public double? PrecipitationMm { get; init; }
    public double? VisibilityKm { get; init; }
    public double? PressureHpa { get; init; }
    public double? CloudCoverPercent { get; init; }
    public string? WeatherPhrase { get; init; }
}

public sealed class WeatherForecastPoint
{
    public DateTime TimestampUtc { get; init; }
    public double? TemperatureC { get; init; }
    public double? PrecipitationMm { get; init; }
    public double? WindSpeedKph { get; init; }
}

public sealed class SevereWeatherAlert
{
    public string AlertId { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public string? Severity { get; init; }
    public string? Description { get; init; }
    public DateTime? StartTimeUtc { get; init; }
    public DateTime? EndTimeUtc { get; init; }
    public string? Source { get; init; }
}

public sealed class TrafficContext
{
    public int IncidentCount { get; init; }
    public int MajorIncidentCount { get; init; }
    public double? NearestIncidentDistanceKm { get; init; }
    public bool SignificantTrafficDisruption { get; init; }
}
