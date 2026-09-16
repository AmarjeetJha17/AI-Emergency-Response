namespace EmergencyResponse.Application.AI.DuplicateDetection;

public static class DuplicateScoringWeights
{
    public const double Semantic = 0.50;
    public const double Geographic = 0.25;
    public const double Temporal = 0.15;
    public const double IncidentType = 0.10;
}