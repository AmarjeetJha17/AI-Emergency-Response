using EmergencyResponse.Domain.Copilot;

namespace EmergencyResponse.Application.Copilot;

public sealed record CreateCopilotConversationRequest(
    Guid? IncidentId,
    Guid? SituationId,
    string? Title);

public sealed record CopilotConversationSummary(
    Guid Id,
    Guid? IncidentId,
    Guid? SituationId,
    string Title,
    string? Summary,
    DateTime LastActivityAtUtc,
    DateTime? ArchivedAtUtc);

public sealed record AddCopilotMessageRequest(
    string Content);

public sealed record CopilotMessageView(
    Guid Id,
    CopilotMessageRole Role,
    string Content,
    DateTime CreatedAtUtc,
    string? GroundingStatus,
    string? CitationsJson,
    int SequenceNumber);

public sealed record AddCopilotFeedbackRequest(
    CopilotFeedbackRating Rating,
    string? Comment);
