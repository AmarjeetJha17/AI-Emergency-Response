using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class OperationalSituation : BaseEntity
{
    public string SituationNumber { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string? Summary { get; set; }

    public EscalationLevel Level { get; set; }

    public EscalationStatus Status { get; set; } = EscalationStatus.Open;

    public double? SituationScore { get; set; }

    public int IncidentCount { get; set; }

    public int CriticalIncidentCount { get; set; }

    public int HighIncidentCount { get; set; }

    public int PeopleAffected { get; set; }

    public int Casualties { get; set; }

    public bool ResourceShortageDetected { get; set; }

    public bool RequiresHumanReview { get; set; } = true;

    public DateTime? LastEvaluatedAtUtc { get; set; }

    public DateTime? LastAIAnalysisAtUtc { get; set; }

    public ICollection<OperationalSituationIncident> Incidents { get; set; } = new List<OperationalSituationIncident>();

    public ICollection<OperationalSituationAnalysis> Analyses { get; set; } = new List<OperationalSituationAnalysis>();
}
