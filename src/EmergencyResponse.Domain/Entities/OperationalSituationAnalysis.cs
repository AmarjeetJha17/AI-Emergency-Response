using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class OperationalSituationAnalysis : BaseEntity
{
    /// <summary>Copied from the owning situation only when that situation is an explicitly owned portfolio fixture.</summary>
    public string? PortfolioDemoOwnershipKey { get; set; }

    public Guid OperationalSituationId { get; set; }

    public OperationalSituation OperationalSituation { get; set; } = null!;

    public string ModelName { get; set; } = null!;

    public string PromptVersion { get; set; } = null!;

    public string InputSnapshotJson { get; set; } = null!;

    public string OutputJson { get; set; } = null!;

    public DateTime GeneratedAtUtc { get; set; }

    public bool HumanReviewed { get; set; }

    public string? HumanDecision { get; set; }
}
