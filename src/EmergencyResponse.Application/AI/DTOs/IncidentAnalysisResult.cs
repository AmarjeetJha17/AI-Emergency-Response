namespace EmergencyResponse.Application.AI.DTOs;

public sealed record IncidentAnalysisResult
{
    public string IncidentType { get; init; } = "Unknown";

    public EmergencyFacts Facts { get; init; } = new();

    public string SeverityCandidate { get; init; } = "Unknown";

    public int? PeopleAffected { get; init; }

    public int? VehiclesAffected { get; init; }

    public int? Casualties { get; init; }

    public string LocationDescription { get; init; }
        = "Unknown";

    public string RecommendedDepartment { get; init; }
        = "Unknown";

    public double Confidence { get; init; }
}