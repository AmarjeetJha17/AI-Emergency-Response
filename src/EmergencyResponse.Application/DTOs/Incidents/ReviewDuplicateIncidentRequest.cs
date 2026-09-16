namespace EmergencyResponse.Application.DTOs.Incidents;

public sealed record ReviewDuplicateIncidentRequest
{
    public bool ConfirmedDuplicate { get; init; }

    public string? ReviewDecision { get; init; }

    public Guid? CanonicalIncidentId { get; init; }
}
