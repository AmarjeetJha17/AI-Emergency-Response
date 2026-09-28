namespace EmergencyResponse.Application.Realtime;

public sealed record EscalationUpdatedEvent
{
    public Guid EscalationId { get; init; }

    public Guid IncidentId { get; init; }

    public string? IncidentNumber { get; init; }

    public string Type { get; init; } = null!;

    public string Level { get; init; } = null!;

    public string Status { get; init; } = null!;

    public string Reason { get; init; } = null!;

    public DateTime UpdatedAtUtc { get; init; }
}
