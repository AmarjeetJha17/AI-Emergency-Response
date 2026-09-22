using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public sealed record AssignableIncidentItem
{
    public Guid Id { get; init; }
    public string IncidentNumber { get; init; } = null!;
    public string Title { get; init; } = null!;
    public SeverityLevel Severity { get; init; }
    public PriorityLevel Priority { get; init; }
    public string? DepartmentName { get; init; }
}
