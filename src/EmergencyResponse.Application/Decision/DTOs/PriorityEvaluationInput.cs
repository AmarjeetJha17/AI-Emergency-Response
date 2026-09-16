using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Decision.DTOs;

public sealed record PriorityEvaluationInput
{
    public SeverityLevel Severity { get; init; }

    public int? PeopleAffected { get; init; }

    public int? Casualties { get; init; }

    public bool PeopleTrapped { get; init; }

    public bool RoadBlocked { get; init; }

    public bool CriticalInfrastructureAffected { get; init; }

    public bool EscalationRisk { get; init; }

    public bool EmergencyServicesRequired { get; init; }
}