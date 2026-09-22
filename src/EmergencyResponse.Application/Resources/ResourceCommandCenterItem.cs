using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public sealed record ResourceCommandCenterItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public ResourceType ResourceType { get; init; }
    public ResourceStatus Status { get; init; }
    public string? DepartmentName { get; init; }
    public Guid? ActiveIncidentId { get; init; }
    public string? ActiveIncidentNumber { get; init; }
    public string? ActiveIncidentTitle { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public DateTime? LocationUpdatedAtUtc { get; init; }
    public DateTime? AssignedAtUtc { get; init; }
    public DateTime? EnRouteAtUtc { get; init; }
    public DateTime? OnSceneAtUtc { get; init; }
}
