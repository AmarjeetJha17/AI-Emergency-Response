namespace EmergencyResponse.Application.DTOs.Incidents;

public sealed record IncidentListItem
{
    public Guid Id { get; init; }

    public string IncidentNumber { get; init; } = null!;

    public string Title { get; init; } = null!;

    public string IncidentType { get; init; } = null!;

    public string Status { get; init; } = null!;

    public string Severity { get; init; } = null!;

    public string Priority { get; init; } = null!;

    public string? City { get; init; }

    public DateTime ReportedAtUtc { get; init; }
}