namespace EmergencyResponse.Application.Realtime;

public sealed record DashboardUpdatedEvent
{
    public string Reason { get; init; } = null!;

    public Guid? IncidentId { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}
