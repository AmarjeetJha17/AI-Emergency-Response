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

    public int SeverityScore { get; init; }

    public IReadOnlyList<string> SeverityReasons { get; init; } = [];

    public string Priority { get; init; } = null!;

    public int PriorityScore { get; init; }

    public IReadOnlyList<string> PriorityReasons { get; init; } = [];

    public decimal Latitude { get; init; }

    public decimal Longitude { get; init; }

    public string? Address { get; init; }

    public string? City { get; init; }

    public string? District { get; init; }

    public string? State { get; init; }

    public int? PeopleAffected { get; init; }

    public int? VehiclesAffected { get; init; }

    public int? Casualties { get; init; }

    public DateTime ReportedAtUtc { get; init; }

    public IReadOnlyList<IncidentMediaResponse> VisualEvidence { get; init; } = [];

    public IReadOnlyList<IncidentStatusHistoryResponse> StatusHistory { get; init; } = [];

    public IReadOnlyList<IncidentAssignedResourceResponse> AssignedResources { get; init; } = [];

    public IReadOnlyList<EmergencyResponse.Application.Escalation.DTOs.IncidentEscalationResponse> ActiveEscalations { get; init; } = [];
}

public sealed record IncidentAssignedResourceResponse
{
    public Guid ResourceId { get; init; }
    public string ResourceName { get; init; } = null!;
    public string ResourceType { get; init; } = null!;
    public string Status { get; init; } = null!;
    public DateTime AssignedAtUtc { get; init; }
    public DateTime? DispatchedAtUtc { get; init; }
    public DateTime? EnRouteAtUtc { get; init; }
    public DateTime? OnSceneAtUtc { get; init; }
    public DateTime? ReturningAtUtc { get; init; }
}
