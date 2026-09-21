using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Map;

public sealed record MapResource
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public ResourceType ResourceType { get; init; }

    public ResourceStatus Status { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string? DepartmentName { get; init; }

    public DateTime? LocationUpdatedAtUtc { get; init; }
}
