using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public sealed class CopilotToolInvocation : BaseEntity
{
    public Guid CopilotInteractionId { get; set; }

    public CopilotInteraction CopilotInteraction { get; set; } = null!;

    public string ToolName { get; set; } = string.Empty;

    public string ArgumentsJson { get; set; } = "{}";

    public string? ResultSummaryJson { get; set; }

    public bool Succeeded { get; set; }

    public string? FailureReason { get; set; }

    public DateTime InvokedAtUtc { get; set; }

    public long ExecutionTimeMs { get; set; }
}
