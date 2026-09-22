using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public sealed record ResourceRecommendation
{
    public Guid ResourceId { get; init; }

    public string ResourceName { get; init; } = null!;

    public ResourceType ResourceType { get; init; }

    public ResourceStatus Status { get; init; }

    public string? DepartmentName { get; init; }

    public double? DistanceKm { get; init; }

    public double RecommendationScore { get; init; }

    public bool Eligible { get; init; }

    public IReadOnlyList<string> Reasons { get; init; } = [];

    public IReadOnlyList<string> ExclusionReasons { get; init; } = [];

    public Guid IncidentId { get; init; }

    public string IncidentNumber { get; init; } = null!;
}
