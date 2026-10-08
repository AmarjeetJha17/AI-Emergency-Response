using EmergencyResponse.Domain.Common;

namespace EmergencyResponse.Domain.Entities;

public sealed class CopilotConversation : BaseEntity
{
    public EmergencyResponse.Domain.Enums.DataClassification DataClassification { get; set; } = EmergencyResponse.Domain.Enums.DataClassification.Confidential;
    public string UserId { get; set; } = string.Empty;

    public Guid? IncidentId { get; set; }

    public Guid? SituationId { get; set; }

    public string Title { get; set; } = "Emergency Copilot Conversation";

    public DateTime LastActivityAtUtc { get; set; }

    public DateTime? ArchivedAtUtc { get; set; }

    public string? Summary { get; set; }

    public ICollection<CopilotMessage> Messages { get; set; } = [];
}
