using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Situations.DTOs;

public sealed class SituationCommandCenterItem
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

    public DateTime LastEvaluatedAtUtc { get; init; }

    public DateTime? LastAIAnalysisAtUtc { get; init; }
}
