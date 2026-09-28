using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Escalation;

public sealed record EscalationEvaluationInput
{
    public Guid IncidentId { get; init; }

    public SeverityLevel CurrentSeverity { get; init; }

    public SeverityLevel? PreviousSeverity { get; init; }

    public int PeopleAffected { get; init; }

    public int Casualties { get; init; }

    public bool PeopleTrapped { get; init; }

    public bool ImmediateDanger { get; init; }

    public bool CriticalInfrastructureAffected { get; init; }

    public bool EscalationRisk { get; init; }

    public int RelatedIncidentCount { get; init; }

    public int DuplicateIncidentCount { get; init; }

    public int RequiredResourceCount { get; init; }

    public int AvailableMatchingResourceCount { get; init; }
}
