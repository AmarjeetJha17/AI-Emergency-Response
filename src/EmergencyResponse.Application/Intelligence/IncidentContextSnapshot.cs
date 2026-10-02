namespace EmergencyResponse.Application.Intelligence;

public sealed class IncidentContextSnapshot
{
    public IncidentContextIncident Incident { get; init; } = new();
    public VisualContext? Visual { get; init; }
    public EnvironmentalContext? Environment { get; init; }
    public double? PredictionProbability { get; init; }
    public string? PredictionModelVersion { get; init; }
    public string? PredictionFeatureSchemaVersion { get; init; }
    public IReadOnlyList<string> VerificationRequired { get; init; } = [];
}

public sealed class IncidentContextIncident
{
    public string IncidentType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int PeopleAffected { get; init; }
    public int VehiclesAffected { get; init; }
    public int Casualties { get; init; }
    public int SeverityScore { get; init; }
    public int PriorityScore { get; init; }
}

public sealed class VisualContext
{
    public string? SceneDescription { get; init; }
    public IReadOnlyList<string> DetectedHazards { get; init; } = [];
    public bool PeopleAppearTrapped { get; init; }
    public bool RoadAppearsBlocked { get; init; }
    public bool StructuralDamageVisible { get; init; }
    public bool FireVisible { get; init; }
    public bool FloodingVisible { get; init; }
    public bool SmokeVisible { get; init; }
    public bool ImmediateDangerVisible { get; init; }
    public double Confidence { get; init; }
}

public sealed class EnvironmentalContext
{
    public double? TemperatureC { get; init; }
    public double? WindSpeedKph { get; init; }
    public double? PrecipitationMm { get; init; }
    public double? VisibilityKm { get; init; }
    public double? RelativeHumidity { get; init; }
    public bool HasSevereWeatherAlert { get; init; }
    public IReadOnlyList<string> SevereWeatherTypes { get; init; } = [];
    public double? AirQualityIndex { get; init; }
}
