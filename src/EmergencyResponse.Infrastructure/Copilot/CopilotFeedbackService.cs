using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Copilot;

public sealed class CopilotFeedbackService : ICopilotFeedbackService
{
    private readonly ApplicationDbContext _db;

    public CopilotFeedbackService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<CopilotFeedback> AddAsync(
        string userId,
        Guid messageId,
        AddCopilotFeedbackRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Comment is { Length: > 1000 })
        {
            throw new ArgumentException(
                "Feedback comments must not exceed 1000 characters.",
                nameof(request));
        }

        var ownsMessage = await _db.CopilotMessages
            .AnyAsync(x =>
                x.Id == messageId &&
                x.CopilotConversation.UserId == userId,
                cancellationToken);
        if (!ownsMessage)
        {
            throw new KeyNotFoundException("Copilot message was not found.");
        }

        var feedback = new CopilotFeedback
        {
            Id = Guid.NewGuid(),
            CopilotMessageId = messageId,
            UserId = userId,
            Rating = request.Rating,
            Comment = request.Comment?.Trim()
        };
        _db.CopilotFeedback.Add(feedback);
        await _db.SaveChangesAsync(cancellationToken);
        return feedback;
    }
}
