namespace EmergencyResponse.Application.Resources;

public sealed record ResourceAssignmentResult
{
    public Guid AssignmentId { get; init; }

    public Guid ResourceId { get; init; }

    public Guid IncidentId { get; init; }

    public DateTime AssignedAtUtc { get; init; }
}
