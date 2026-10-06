namespace EmergencyResponse.Application.Knowledge;

public sealed class KnowledgeAccessContext
{
    public string UserId { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    public Guid? DepartmentId { get; init; }
}
