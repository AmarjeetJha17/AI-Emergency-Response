using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public sealed record ResourceDetailsResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public ResourceType ResourceType { get; init; }

    public ResourceStatus Status { get; init; }

    public string? DepartmentName { get; init; }

    public int? Capacity { get; init; }

    public bool ShowOnOperationalMap { get; init; } = true;

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public DateTime? LocationUpdatedAtUtc { get; init; }

    public Guid? ActiveIncidentId { get; init; }

    public string? ActiveIncidentNumber { get; init; }

    public string? ActiveIncidentTitle { get; init; }

    public DateTime? AssignedAtUtc { get; init; }

    public DateTime? DispatchedAtUtc { get; init; }

    public DateTime? EnRouteAtUtc { get; init; }

    public DateTime? OnSceneAtUtc { get; init; }

    public DateTime? ReturningAtUtc { get; init; }

    public string? AssignedByUserId { get; init; }

    public string? AssignmentRemarks { get; init; }

    public IReadOnlyList<ResourceStatusHistoryItem> StatusHistory { get; init; } = [];
}

public sealed record ResourceStatusHistoryItem
{
    public ResourceStatus PreviousStatus { get; init; }
    public ResourceStatus NewStatus { get; init; }
    public string? Reason { get; init; }
    public string? ChangedByUserId { get; init; }
    public DateTime ChangedAtUtc { get; init; }
    public string? IncidentNumber { get; init; }
}
