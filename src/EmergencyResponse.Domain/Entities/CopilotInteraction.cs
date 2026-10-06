using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Knowledge;

namespace EmergencyResponse.Domain.Entities;

public sealed class CopilotInteraction : BaseEntity
{
    public string UserId { get; set; } = string.Empty;

    public string Question { get; set; } = string.Empty;

    public Guid? IncidentId { get; set; }

    public Guid? SituationId { get; set; }

    public string ModelName { get; set; } = string.Empty;

    public string? ModelVersion { get; set; }

    public string PromptVersion { get; set; } = string.Empty;

    public string RetrievedDocumentIdsJson { get; set; } = "[]";

    public string Answer { get; set; } = string.Empty;

    public string CitationsJson { get; set; } = "[]";

    public GroundingStatus GroundingStatus { get; set; }

    public ICollection<CopilotToolInvocation> ToolInvocations { get; set; } = [];
}
