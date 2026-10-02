namespace EmergencyResponse.Application.Predictive.Escalation;

public sealed class EscalationPredictionFeatures
{
    public float SeverityScore { get; init; }

    public float PriorityScore { get; init; }

    public float PeopleAffected { get; init; }

    public float VehiclesAffected { get; init; }

    public float Casualties { get; init; }

    public float ImmediateDanger { get; init; }

    public float PeopleTrapped { get; init; }

    public float CriticalInfrastructureAffected { get; init; }

    public float InfrastructureDamage { get; init; }

    public float RoadBlocked { get; init; }

    public float EscalationCount { get; init; }

    public float DuplicateCount { get; init; }

    public float RelatedIncidentCount { get; init; }

    public float ResourceShortage { get; init; }

    public float IncidentAgeMinutes { get; init; }

    public float PrecipitationMm { get; init; }

    public float WindSpeedKph { get; init; }

    public float VisibilityKm { get; init; }

    public float SevereWeatherAlert { get; init; }

    public float AirQualityIndex { get; init; }

    public float SignificantTrafficDisruption { get; init; }

    public bool EnvironmentalContextAvailable { get; init; }
}
