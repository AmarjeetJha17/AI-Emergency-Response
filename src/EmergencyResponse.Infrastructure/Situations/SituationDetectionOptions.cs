namespace EmergencyResponse.Infrastructure.Situations;

public sealed class SituationDetectionOptions
{
    public const string SectionName = "SituationDetection";

    public double MaxClusterDistanceKm { get; set; } = 10.0;

    public double MaxTimeWindowHours { get; set; } = 6.0;

    public int MinIncidentCount { get; set; } = 3;

    public double OverlapThreshold { get; set; } = 0.50;
}
