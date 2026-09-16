namespace EmergencyResponse.Application.AI.DTOs;

public sealed record DuplicateIncidentCandidate
{
    public Guid MatchId { get; init; }

    public Guid IncidentId { get; init; }

    public string IncidentNumber { get; init; } = null!;

    public string Title { get; init; } = null!;

    public double SemanticSimilarity { get; init; }

    public double GeographicSimilarity { get; init; }

    public double TemporalSimilarity { get; init; }

    public double IncidentTypeSimilarity { get; init; }

    public double DuplicateScore { get; init; }

    public double DistanceKm { get; init; }

    public TimeSpan TimeDifference { get; init; }

    public bool RequiresReview { get; init; }
}