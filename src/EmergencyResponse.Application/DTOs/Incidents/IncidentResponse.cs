using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.DTOs.Incidents;

public sealed record IncidentResponse
{
    public Guid Id { get; init; }

    public string IncidentNumber { get; init; } = null!;

    public string Title { get; init; } = null!;

    public string Description { get; init; } = null!;

    public string IncidentType { get; init; } = null!;

    public string Status { get; init; } = null!;

    public DuplicateDetectionStatus DuplicateDetectionStatus { get; init; }

    public bool HasDuplicateCandidates { get; init; }

    public Guid? CanonicalIncidentId { get; init; }

    public string? CanonicalIncidentNumber { get; init; }

    public IReadOnlyList<RelatedIncidentResponse> RelatedIncidents { get; init; }
        = Array.Empty<RelatedIncidentResponse>();

    public string Severity { get; init; } = null!;

    public string Priority { get; init; } = null!;

    public decimal Latitude { get; init; }

    public decimal Longitude { get; init; }

    public string? Address { get; init; }

    public string? City { get; init; }

    public int? PeopleAffected { get; init; }

    public int? VehiclesAffected { get; init; }

    public int? Casualties { get; init; }

    public DateTime ReportedAtUtc { get; init; }

    public IReadOnlyList<IncidentMediaResponse> VisualEvidence { get; init; } = [];
}