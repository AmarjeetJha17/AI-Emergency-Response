namespace EmergencyResponse.Application.DTOs.IncidentTypes;

public sealed record IncidentTypeResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Code { get; init; } = null!;
}