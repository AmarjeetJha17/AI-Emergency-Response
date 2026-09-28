using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Situations.DTOs;

public sealed class SituationMapItem
{
    public Guid Id { get; init; }

    public string SituationNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string Summary { get; init; } = string.Empty;

    public EscalationLevel Level { get; init; }

    public EscalationStatus Status { get; init; }

    public double SituationScore { get; init; }

    public int IncidentCount { get; init; }

    public int CriticalIncidentCount { get; init; }

    public int HighIncidentCount { get; init; }

    public int PeopleAffected { get; init; }

    public int Casualties { get; init; }

    public bool ResourceShortageDetected { get; init; }

    public bool RequiresHumanReview { get; init; }

    public double CenterLatitude { get; init; }

    public double CenterLongitude { get; init; }

    public double RadiusMeters { get; init; }

    public Guid? PrimaryIncidentId { get; init; }

    public string? PrimaryIncidentNumber { get; init; }

    public IReadOnlyList<SituationMapIncident> Incidents { get; init; } = [];
}

public sealed class SituationMapIncident
{
    public Guid IncidentId { get; init; }

    public string IncidentNumber { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string IncidentType { get; init; } = string.Empty;

    public SeverityLevel Severity { get; init; }

    public PriorityLevel Priority { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public bool IsPrimaryIncident { get; init; }
}
