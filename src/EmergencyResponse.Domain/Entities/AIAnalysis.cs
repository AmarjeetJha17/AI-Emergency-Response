using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public class AIAnalysis : BaseEntity
{
    public Guid IncidentId { get; set; }

    public Incident Incident { get; set; } = null!;

    public string ModelName { get; set; } = null!;

    public string? ModelVersion { get; set; }

    public string PromptVersion { get; set; } = null!;

    public string InputReference { get; set; } = null!;

    public string OutputJson { get; set; } = null!;

    public double? Confidence { get; set; }

    public long ExecutionTimeMs { get; set; }

    public bool HumanReviewed { get; set; }

    public string? HumanDecision { get; set; }
}