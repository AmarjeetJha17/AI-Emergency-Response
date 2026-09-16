namespace EmergencyResponse.Web.Models.Incidents;

public sealed class DuplicateIncidentReviewViewModel
{
    public Guid MatchId { get; init; }

    public Guid SourceIncidentId { get; init; }

    public Guid CandidateIncidentId { get; init; }

    public string SourceIncidentNumber { get; init; } = null!;

    public string CandidateIncidentNumber { get; init; } = null!;

    public string SourceTitle { get; init; } = null!;

    public string CandidateTitle { get; init; } = null!;

    public string? SourceDescription { get; init; }

    public string? CandidateDescription { get; init; }

    public string? SourceLocation { get; init; }

    public string? CandidateLocation { get; init; }

    public string? SourceIncidentType { get; init; }

    public string? CandidateIncidentType { get; init; }

    public double SemanticSimilarity { get; init; }

    public double GeographicSimilarity { get; init; }

    public double TemporalSimilarity { get; init; }

    public double IncidentTypeSimilarity { get; init; }

    public double DuplicateScore { get; init; }

    public double DistanceKm { get; init; }

    public TimeSpan TimeDifference { get; init; }

    public bool Reviewed { get; init; }

    public bool ConfirmedDuplicate { get; init; }

    public string? ReviewDecision { get; init; }
}