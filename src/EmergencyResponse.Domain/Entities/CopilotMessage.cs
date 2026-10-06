using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Copilot;

namespace EmergencyResponse.Domain.Entities;

public sealed class CopilotMessage : BaseEntity
{
    public Guid CopilotConversationId { get; set; }

    public CopilotConversation CopilotConversation { get; set; } = null!;

    public CopilotMessageRole Role { get; set; }

    public string Content { get; set; } = string.Empty;

    public string? ModelName { get; set; }

    public string? ToolCallsJson { get; set; }

    public string? CitationsJson { get; set; }

    public string? GroundingStatus { get; set; }

    public int SequenceNumber { get; set; }
}
