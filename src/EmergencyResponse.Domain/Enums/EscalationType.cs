namespace EmergencyResponse.Domain.Enums;

public enum EscalationType
{
    SeverityIncrease = 1,
    PeopleInDanger = 2,
    ImmediateDanger = 3,
    CriticalInfrastructure = 4,
    CasualtyIncrease = 5,
    IncidentCluster = 6,
    ResourceShortage = 7,
    DuplicateIncidentCluster = 8,
    ManualEscalation = 9
}
