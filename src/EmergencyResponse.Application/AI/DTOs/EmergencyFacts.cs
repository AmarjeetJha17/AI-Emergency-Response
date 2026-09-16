namespace EmergencyResponse.Application.AI.DTOs;

public sealed record EmergencyFacts
{
    public bool PeopleTrapped { get; init; }

    public bool RoadBlocked { get; init; }

    public bool ResidentialAreaAffected { get; init; }

    public bool InfrastructureDamage { get; init; }

    public bool ImmediateDanger { get; init; }

    public bool CriticalInfrastructureAffected { get; init; }

    public bool EscalationRisk { get; init; }
}