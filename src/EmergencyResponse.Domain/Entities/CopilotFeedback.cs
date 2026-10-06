using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Copilot;

namespace EmergencyResponse.Domain.Entities;

public sealed class CopilotFeedback : BaseEntity
{
    public Guid CopilotMessageId { get; set; }

    public CopilotMessage CopilotMessage { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;

    public CopilotFeedbackRating Rating { get; set; }

    public string? Comment { get; set; }
}
