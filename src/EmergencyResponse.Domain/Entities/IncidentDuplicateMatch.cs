using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class IncidentDuplicateMatch : BaseEntity
{
    public Guid SourceIncidentId { get; set; }

    public Incident SourceIncident { get; set; } = null!;

    public Guid CandidateIncidentId { get; set; }

    public Incident CandidateIncident { get; set; } = null!;

    public double SemanticSimilarity { get; set; }

    public double GeographicSimilarity { get; set; }

    public double TemporalSimilarity { get; set; }

    public double IncidentTypeSimilarity { get; set; }

    public double DuplicateScore { get; set; }

    public bool Reviewed { get; set; }

    public bool ConfirmedDuplicate { get; set; }

    public string? ReviewDecision { get; set; }

    public string? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAtUtc { get; set; }
}