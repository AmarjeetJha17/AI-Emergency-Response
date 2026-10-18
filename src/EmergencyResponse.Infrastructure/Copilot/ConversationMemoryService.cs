using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Copilot;

public sealed class ConversationMemoryService : IConversationMemoryService
{
    private readonly ApplicationDbContext _db;
    private readonly CopilotMemoryOptions _options;

    public ConversationMemoryService(
        ApplicationDbContext db,
        IOptions<CopilotMemoryOptions> options)
    {
        _db = db;
        _options = options.Value;
    }

    public async Task<CopilotConversation> GetOrCreateAsync(
        string userId,
        Guid? incidentId,
        Guid? situationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await _db.CopilotConversations
            .FirstOrDefaultAsync(x =>
                x.UserId == userId &&
                x.IncidentId == incidentId &&
                x.SituationId == situationId &&
                x.ArchivedAtUtc == null,
                cancellationToken);
        if (conversation is not null)
        {
            return conversation;
        }

        conversation = new CopilotConversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            IncidentId = incidentId,
            SituationId = situationId,
            LastActivityAtUtc = DateTime.UtcNow
        };
        _db.CopilotConversations.Add(conversation);
        await _db.SaveChangesAsync(cancellationToken);
        return conversation;
    }

    public Task<CopilotConversation?> GetOwnedAsync(
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken = default) =>
        _db.CopilotConversations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == conversationId && x.UserId == userId,
                cancellationToken);

    public async Task<IReadOnlyList<CopilotConversation>> GetRecentAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        await _db.CopilotConversations
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LastActivityAtUtc)
            .Take(50)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CopilotMessage>> GetRecentMessagesAsync(
        string userId,
        Guid conversationId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var owned = await GetOwnedAsync(userId, conversationId, cancellationToken);
        if (owned is null)
        {
            throw new KeyNotFoundException("Conversation was not found.");
        }

        take = Math.Clamp(take, 1, _options.RecentMessageCount);
        return await _db.CopilotMessages
            .AsNoTracking()
            .Where(x => x.CopilotConversationId == conversationId)
            .OrderByDescending(x => x.SequenceNumber)
            .Take(take)
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task AddMessageAsync(
        string userId,
        Guid conversationId,
        CopilotMessage message,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetOwnedTrackedAsync(userId, conversationId, cancellationToken);
        if (conversation is null)
        {
            throw new KeyNotFoundException("Conversation was not found.");
        }

        var count = await _db.CopilotMessages
            .CountAsync(x => x.CopilotConversationId == conversationId, cancellationToken);
        if (count >= _options.MaxConversationMessages)
        {
            throw new InvalidOperationException("Conversation message limit reached.");
        }

        message.Id = Guid.NewGuid();
        message.CopilotConversationId = conversationId;
        message.SequenceNumber = count + 1;
        _db.CopilotMessages.Add(message);
        conversation.LastActivityAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task ArchiveAsync(
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetOwnedTrackedAsync(userId, conversationId, cancellationToken);
        if (conversation is null)
        {
            throw new KeyNotFoundException("Conversation was not found.");
        }

        conversation.ArchivedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RenameAsync(
        string userId,
        Guid conversationId,
        string title,
        CancellationToken cancellationToken = default)
    {
        var conversation = await GetOwnedTrackedAsync(userId, conversationId, cancellationToken);
        if (conversation is null)
        {
            throw new KeyNotFoundException("Conversation was not found.");
        }

        conversation.Title = title.Trim()[..Math.Min(200, title.Trim().Length)];
        await _db.SaveChangesAsync(cancellationToken);
    }

    private Task<CopilotConversation?> GetOwnedTrackedAsync(
        string userId,
        Guid conversationId,
        CancellationToken cancellationToken) =>
        _db.CopilotConversations
            .FirstOrDefaultAsync(
                x => x.Id == conversationId && x.UserId == userId,
                cancellationToken);
}
