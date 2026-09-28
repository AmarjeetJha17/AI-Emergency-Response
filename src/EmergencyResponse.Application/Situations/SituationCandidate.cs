using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Situations;

public sealed record SituationCandidate
{
    public Guid IncidentId { get; init; }

    public string IncidentNumber { get; init; } = null!;

    public string Title { get; init; } = null!;

    public Guid IncidentTypeId { get; init; }

    public string? IncidentTypeName { get; init; }

    public SeverityLevel Severity { get; init; }

    public PriorityLevel Priority { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public int PeopleAffected { get; init; }

    public int Casualties { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
