using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Decision.DTOs;

public sealed record SeverityEvaluationInput
{
    public string IncidentType { get; init; } = "Unknown";

    public int? PeopleAffected { get; init; }

    public int? VehiclesAffected { get; init; }

    public int? Casualties { get; init; }

    public bool PeopleTrapped { get; init; }

    public bool InfrastructureDamage { get; init; }

    public bool RoadBlocked { get; init; }

    public bool ResidentialAreaAffected { get; init; }

    public bool ImmediateDanger { get; init; }

    public SeverityLevel? AICandidateSeverity { get; init; }

    public double? AIConfidence { get; init; }
}