using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Map;

public sealed record EmergencyMapIncident
{
    public Guid Id { get; init; }

    public string IncidentNumber { get; init; } = null!;

    public string Title { get; init; } = null!;

    public string? IncidentType { get; init; }

    public IncidentStatus Status { get; init; }

    public SeverityLevel Severity { get; init; }

    public PriorityLevel Priority { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string? Address { get; init; }

    public string? DepartmentName { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public bool HasDuplicateCandidates { get; init; }

    public bool HasVisualReview { get; init; }

    public bool RequiresAIReview { get; init; }
}
