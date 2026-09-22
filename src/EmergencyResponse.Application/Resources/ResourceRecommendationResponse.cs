namespace EmergencyResponse.Application.Resources;

public sealed record ResourceRecommendationResponse
{
    public Guid IncidentId { get; init; }

    public string? IncidentNumber { get; init; }

    public string? IncidentTitle { get; init; }

    public string? IncidentType { get; init; }

    public string? Severity { get; init; }

    public string? Priority { get; init; }

    public string? DepartmentName { get; init; }

    public IReadOnlyList<ResourceRequirementDto> Requirements { get; init; } = [];

    public IReadOnlyList<ResourceRecommendation> Recommendations { get; init; } = [];
}
