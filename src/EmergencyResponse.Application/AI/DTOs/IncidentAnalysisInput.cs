namespace EmergencyResponse.Application.AI.DTOs;

public sealed record IncidentAnalysisInput
{
    public string Title { get; init; } = null!;

    public string Description { get; init; } = null!;

    public string? IncidentType { get; init; }

    public decimal Latitude { get; init; }

    public decimal Longitude { get; init; }

    public string? Address { get; init; }

    public string? City { get; init; }

    public string? District { get; init; }

    public string? State { get; init; }

    public int? PeopleAffected { get; init; }

    public int? VehiclesAffected { get; init; }

    public int? Casualties { get; init; }
}