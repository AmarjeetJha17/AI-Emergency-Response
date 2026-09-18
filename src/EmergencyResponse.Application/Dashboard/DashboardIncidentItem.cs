using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Dashboard;

public sealed record DashboardIncidentItem
{
    public Guid Id { get; init; }

    public string IncidentNumber { get; init; } = null!;

    public string Title { get; init; } = null!;

    public string? IncidentType { get; init; }

    public IncidentStatus Status { get; init; }

    public SeverityLevel Severity { get; init; }

    public PriorityLevel Priority { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public bool IsAssigned { get; init; }

    public bool RequiresAIReview { get; init; }

    public bool HasDuplicateCandidates { get; init; }

    public bool HasVisualReview { get; init; }
}
