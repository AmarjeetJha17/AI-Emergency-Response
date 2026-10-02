using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public sealed class EnvironmentalGridSnapshot : BaseEntity
{
    public string CellKey { get; set; } = string.Empty;

    public DateTime ObservedAtUtc { get; set; }

    public string Provider { get; set; } = string.Empty;

    public double? TemperatureC { get; set; }

    public double? WindSpeedKph { get; set; }

    public double? PrecipitationMm { get; set; }

    public double? VisibilityKm { get; set; }

    public double? AirQualityIndex { get; set; }

    public bool HasSevereWeatherAlert { get; set; }

    public string? SevereAlertTypesJson { get; set; }

    public string? TrafficContextJson { get; set; }

    public string ContextHash { get; set; } = string.Empty;
}
