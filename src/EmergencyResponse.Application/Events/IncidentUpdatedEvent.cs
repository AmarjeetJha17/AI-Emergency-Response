using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Events;

public sealed record IncidentUpdatedEvent
{
    public Guid IncidentId { get; init; }

    public string IncidentNumber { get; init; } = null!;

    public string Status { get; init; } = null!;

    public bool HasDuplicateCandidates { get; init; }

    public DuplicateDetectionStatus DuplicateDetectionStatus { get; init; }

    public string Severity { get; init; } = null!;

    public int SeverityScore { get; init; }

    public string Priority { get; init; } = null!;

    public int PriorityScore { get; init; }

    public string UpdateType { get; init; } = null!;
}