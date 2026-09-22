using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public sealed record ResourceDetailsResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public ResourceType ResourceType { get; init; }

    public ResourceStatus Status { get; init; }

    public string? DepartmentName { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public DateTime? LocationUpdatedAtUtc { get; init; }

    public Guid? ActiveIncidentId { get; init; }

    public string? ActiveIncidentNumber { get; init; }
}
