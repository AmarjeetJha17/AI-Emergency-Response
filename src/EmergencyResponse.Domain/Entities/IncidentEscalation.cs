using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Domain.Entities;

public class IncidentEscalation : BaseEntity
{
    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public EscalationType Type { get; set; }

    public EscalationLevel Level { get; set; }

    public EscalationStatus Status { get; set; } = EscalationStatus.Open;

    public string Reason { get; set; } = null!;

    public string? EvidenceJson { get; set; }

    public double? Score { get; set; }

    public bool RequiresHumanReview { get; set; } = true;

    public string? AcknowledgedByUserId { get; set; }

    public DateTime? AcknowledgedAtUtc { get; set; }

    public string? ResolvedByUserId { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public string? ResolutionNotes { get; set; }
}
