namespace EmergencyResponse.Domain.Copilot;

public enum CopilotActionType
{
    CreateIncidentNote = 1,
    AddIncidentTag = 2,
    AssignResource = 10,
    DispatchResource = 11,
    MarkResourceEnRoute = 12,
    MarkResourceOnScene = 13,
    AcknowledgeEscalation = 20,
    ResolveEscalation = 21,
    AcknowledgeSituation = 30,
    ResolveSituation = 31
}
