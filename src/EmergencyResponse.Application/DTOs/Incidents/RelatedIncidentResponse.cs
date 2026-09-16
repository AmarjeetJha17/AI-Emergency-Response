namespace EmergencyResponse.Application.DTOs.Incidents;

public sealed record RelatedIncidentResponse
{
    public Guid Id { get; init; }

    public string IncidentNumber { get; init; } = null!;

    public string RelationType { get; init; } = null!;
}
