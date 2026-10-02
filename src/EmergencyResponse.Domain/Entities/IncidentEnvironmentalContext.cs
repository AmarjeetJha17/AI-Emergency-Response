using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public sealed class IncidentEnvironmentalContext : BaseEntity
{
    public Guid IncidentId { get; set; }

    public DateTime ObservedAtUtc { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ProviderVersion { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double? TemperatureC { get; set; }

    public double? ApparentTemperatureC { get; set; }

    public double? RelativeHumidity { get; set; }

    public double? WindSpeedKph { get; set; }

    public double? WindDirectionDegrees { get; set; }

    public double? PrecipitationMm { get; set; }

    public double? VisibilityKm { get; set; }

    public double? PressureHpa { get; set; }

    public double? CloudCoverPercent { get; set; }

    public string? WeatherPhrase { get; set; }

    public bool HasSevereWeatherAlert { get; set; }

    public string? SevereWeatherAlertTypesJson { get; set; }

    public string? AirQualityCategory { get; set; }

    public double? AirQualityIndex { get; set; }

    public string? TrafficIncidentsJson { get; set; }

    public string? RawResponseJson { get; set; }

    public string ContextHash { get; set; } = string.Empty;
}
