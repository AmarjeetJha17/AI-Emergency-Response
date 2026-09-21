namespace EmergencyResponse.Application.Map;

public sealed record MapOperationalZone
{
    public Guid IncidentId { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public double RadiusMeters { get; init; }

    public string Severity { get; init; } = null!;

    public string Label { get; init; } = null!;
}
