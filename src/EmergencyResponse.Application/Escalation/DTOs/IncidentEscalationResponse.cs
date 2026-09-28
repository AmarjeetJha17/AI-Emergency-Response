using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Escalation.DTOs;

public sealed record IncidentEscalationResponse
{
    public Guid Id { get; init; }

    public Guid IncidentId { get; init; }

    public string? IncidentNumber { get; init; }

    public string? IncidentTitle { get; init; }

    public EscalationType Type { get; init; }

    public string TypeName { get; init; } = null!;

    public EscalationLevel Level { get; init; }

    public string LevelName { get; init; } = null!;

    public EscalationStatus Status { get; init; }

    public string StatusName { get; init; } = null!;

    public string Reason { get; init; } = null!;

    public IReadOnlyList<string> Evidence { get; init; } = [];

    public double? Score { get; init; }

    public bool RequiresHumanReview { get; init; }

    public string? AcknowledgedByUserId { get; init; }

    public string? AcknowledgedByUserName { get; init; }

    public DateTime? AcknowledgedAtUtc { get; init; }

    public string? ResolvedByUserId { get; init; }

    public string? ResolvedByUserName { get; init; }

    public DateTime? ResolvedAtUtc { get; init; }

    public string? ResolutionNotes { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
