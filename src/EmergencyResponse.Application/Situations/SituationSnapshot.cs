using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Situations;

public sealed record SituationSnapshot
{
    public string SituationNumber { get; init; } = string.Empty;

    public int IncidentCount { get; init; }

    public int CriticalIncidentCount { get; init; }

    public int HighIncidentCount { get; init; }

    public EscalationLevel Level { get; init; }

    public double SituationScore { get; init; }

    public int PeopleAffected { get; init; }

    public int Casualties { get; init; }

    public bool ResourceShortageDetected { get; init; }

    public IReadOnlyList<IncidentSnapshotItem> Incidents { get; init; } = [];
}

public sealed record IncidentSnapshotItem
{
    public Guid IncidentId { get; init; }

    public string IncidentNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string? IncidentTypeName { get; init; }

    public SeverityLevel Severity { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public int? PeopleAffected { get; init; }

    public int? Casualties { get; init; }

    public int ActiveEscalationsCount { get; init; }
}
