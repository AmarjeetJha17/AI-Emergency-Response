namespace EmergencyResponse.Application.Copilot;

public sealed class CopilotQuery
{
    public string Question { get; init; } = string.Empty;

    public Guid? IncidentId { get; init; }

    public Guid? SituationId { get; init; }

    public Guid? ConversationId { get; init; }
}
