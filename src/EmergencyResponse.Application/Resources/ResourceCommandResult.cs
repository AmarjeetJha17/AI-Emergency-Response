using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public sealed record ResourceCommandResult
{
    public Guid ResourceId { get; init; }

    public Guid? IncidentId { get; init; }

    public ResourceStatus PreviousStatus { get; init; }

    public ResourceStatus NewStatus { get; init; }

    public DateTime ChangedAtUtc { get; init; }

    public DateTime? AssignmentTimestampUtc { get; init; }

    public string? IncidentNumber { get; init; }
}
