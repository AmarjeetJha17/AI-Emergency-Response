using EmergencyResponse.Domain.Entities;

namespace EmergencyResponse.Application.Copilot;

public interface ICopilotFeedbackService
{
    Task<CopilotFeedback> AddAsync(
        string userId,
        Guid messageId,
        AddCopilotFeedbackRequest request,
        CancellationToken cancellationToken = default);
}
