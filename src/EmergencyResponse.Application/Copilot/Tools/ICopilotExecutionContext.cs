namespace EmergencyResponse.Application.Copilot.Tools;

public interface ICopilotExecutionContext
{
    string? UserId { get; }

    IReadOnlyList<string> Roles { get; }

    Guid? DepartmentId { get; }

    Guid? IncidentId { get; }

    Guid? SituationId { get; }

    Guid InteractionId { get; }

    bool IsAuthenticated { get; }
}

public sealed class CopilotExecutionContext : ICopilotExecutionContext
{
    public string? UserId { get; private set; }

    public IReadOnlyList<string> Roles { get; private set; } = [];

    public Guid? DepartmentId { get; private set; }

    public Guid? IncidentId { get; private set; }

    public Guid? SituationId { get; private set; }

    public Guid InteractionId { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(UserId);

    public void Set(
        string? userId,
        IReadOnlyList<string> roles,
        Guid? departmentId,
        Guid? incidentId,
        Guid? situationId,
        Guid interactionId)
    {
        UserId = userId;
        Roles = roles;
        DepartmentId = departmentId;
        IncidentId = incidentId;
        SituationId = situationId;
        InteractionId = interactionId;
    }
}
