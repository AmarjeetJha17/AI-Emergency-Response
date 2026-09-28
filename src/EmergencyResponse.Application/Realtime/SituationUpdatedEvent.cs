using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Realtime;

public sealed record SituationUpdatedEvent
{
    public Guid SituationId { get; init; }

    public string SituationNumber { get; init; } = null!;

    public string Title { get; init; } = null!;

    public string Reason { get; init; } = null!;

    public EscalationLevel Level { get; init; }

    public EscalationStatus Status { get; init; }

    public int IncidentCount { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}
