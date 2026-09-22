namespace EmergencyResponse.Application.Realtime;

public sealed record ResourceUpdatedEvent
{
    public Guid ResourceId { get; init; }

    public Guid? IncidentId { get; init; }

    public string Reason { get; init; } = null!;

    public DateTime UpdatedAtUtc { get; init; }
}
