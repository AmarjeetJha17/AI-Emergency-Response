using EmergencyResponse.Domain.Entities;

namespace EmergencyResponse.Application.Copilot;

public interface IConversationMemoryService
{
    Task<CopilotConversation> GetOrCreateAsync(
        string userId,
        Guid? incidentId,
        Guid? situationId,
        CancellationToken cancellationToken = default);

    Task<CopilotConversation?> GetOwnedAsync(
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CopilotConversation>> GetRecentAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CopilotMessage>> GetRecentMessagesAsync(
        string userId,
        Guid conversationId,
        int take,
        CancellationToken cancellationToken = default);

    Task AddMessageAsync(
        string userId,
        Guid conversationId,
        CopilotMessage message,
        CancellationToken cancellationToken = default);

    Task ArchiveAsync(
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken = default);

    Task RenameAsync(
        string userId,
        Guid conversationId,
        string title,
        CancellationToken cancellationToken = default);
}
