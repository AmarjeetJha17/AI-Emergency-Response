using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Situations.DTOs;

public sealed record OperationalSituationResponse
{
    public Guid Id { get; init; }

    public string SituationNumber { get; init; } = null!;

    public string Title { get; init; } = null!;

    public string? Summary { get; init; }

    public EscalationLevel Level { get; init; }

    public string LevelName => Level.ToString();

    public EscalationStatus Status { get; init; }

    public string StatusName => Status.ToString();

    public double? SituationScore { get; init; }

    public int IncidentCount { get; init; }

    public int CriticalIncidentCount { get; init; }

    public int HighIncidentCount { get; init; }

    public int PeopleAffected { get; init; }

    public int Casualties { get; init; }

    public bool ResourceShortageDetected { get; init; }

    public bool RequiresHumanReview { get; init; }

    public DateTime? LastEvaluatedAtUtc { get; init; }

    public DateTime? LastAIAnalysisAtUtc { get; init; }

    public SituationAnalysisResult? LatestAnalysis { get; init; }

    public IReadOnlyList<OperationalSituationIncidentItem> Incidents { get; init; } = [];
}

public sealed record OperationalSituationIncidentItem
{
    public Guid IncidentId { get; init; }

    public string IncidentNumber { get; init; } = null!;

    public string Title { get; init; } = null!;

    public string? IncidentTypeName { get; init; }

    public SeverityLevel Severity { get; init; }

    public PriorityLevel Priority { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public int PeopleAffected { get; init; }

    public int Casualties { get; init; }

    public bool IsPrimaryIncident { get; init; }

    public DateTime AddedAtUtc { get; init; }
}
