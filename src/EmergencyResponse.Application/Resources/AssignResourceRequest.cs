namespace EmergencyResponse.Application.Resources;

public sealed record AssignResourceRequest
{
    public Guid IncidentId { get; init; }

    public string? Notes { get; init; }
}
