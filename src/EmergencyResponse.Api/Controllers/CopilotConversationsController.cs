using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Api.Common;
using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Domain.Copilot;
using EmergencyResponse.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/copilot/conversations")]
[Authorize(Roles =
    $"{ApplicationRoles.Responder},{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
public sealed class CopilotConversationsController : ControllerBase
{
    private readonly IConversationMemoryService _memory;
    private readonly ICopilotFeedbackService _feedback;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmergencyCopilotService _copilot;

    public CopilotConversationsController(
        IConversationMemoryService memory,
        ICopilotFeedbackService feedback,
        ICurrentUserService currentUser,
        IEmergencyCopilotService copilot)
    {
        _memory = memory;
        _feedback = feedback;
        _currentUser = currentUser;
        _copilot = copilot;
    }

    [HttpPost]
    public async Task<ActionResult<CopilotConversationSummary>> Create(
        CreateCopilotConversationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        var conversation = await _memory.GetOrCreateAsync(
            userId,
            request.IncidentId,
            request.SituationId,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            await _memory.RenameAsync(
                userId,
                conversation.Id,
                request.Title,
                cancellationToken);
            conversation.Title = request.Title.Trim()[..Math.Min(200, request.Title.Trim().Length)];
        }

        return Ok(ToSummary(conversation));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CopilotConversationSummary>>> List(
        CancellationToken cancellationToken)
    {
        var conversations = await _memory.GetRecentAsync(
            RequireUserId(),
            cancellationToken);
        return Ok(conversations.Select(ToSummary).ToList());
    }

    [HttpGet("{conversationId:guid}")]
    public async Task<ActionResult<CopilotConversationSummary>> Get(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await _memory.GetOwnedAsync(
            RequireUserId(),
            conversationId,
            cancellationToken);
        return conversation is null
            ? NotFound()
            : Ok(ToSummary(conversation));
    }

    [HttpPost("{conversationId:guid}/archive")]
    public async Task<IActionResult> Archive(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var conversation = await _memory.GetOwnedAsync(
            RequireUserId(),
            conversationId,
            cancellationToken);
        if (conversation is null)
        {
            return NotFound();
        }

        await _memory.ArchiveAsync(
            RequireUserId(),
            conversationId,
            cancellationToken);
        return NoContent();
    }

    [HttpGet("{conversationId:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<CopilotMessageView>>> Messages(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var messages = await _memory.GetRecentMessagesAsync(
            RequireUserId(),
            conversationId,
            take: 100,
            cancellationToken);
        return Ok(messages
            .Where(x => x.Role is CopilotMessageRole.User or CopilotMessageRole.Assistant)
            .Select(x => new CopilotMessageView(
                x.Id,
                x.Role,
                x.Content,
                x.CreatedAtUtc,
                x.GroundingStatus,
                x.CitationsJson,
                x.SequenceNumber))
            .ToList());
    }

    [HttpPost("{conversationId:guid}/messages")]
    [ServiceFilter(typeof(AIEnabledActionFilter))]
    public async Task<ActionResult<CopilotResponse>> Message(
        Guid conversationId,
        AddCopilotMessageRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Message content is required.");
        }

        var conversation = await _memory.GetOwnedAsync(
            RequireUserId(),
            conversationId,
            cancellationToken);
        if (conversation is null)
        {
            return NotFound();
        }

        return Ok(await _copilot.QueryAsync(
            new CopilotQuery
            {
                ConversationId = conversationId,
                Question = request.Content
            },
            cancellationToken));
    }

    [HttpPost("messages/{messageId:guid}/feedback")]
    public async Task<ActionResult> Feedback(
        Guid messageId,
        AddCopilotFeedbackRequest request,
        CancellationToken cancellationToken)
    {
        var userId = RequireUserId();
        try
        {
            var feedback = await _feedback.AddAsync(
                userId,
                messageId,
                request,
                cancellationToken);
            return Ok(new
            {
                feedback.Id,
                feedback.Rating,
                feedback.CreatedAtUtc
            });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private string RequireUserId() =>
        _currentUser.UserId
        ?? throw new UnauthorizedAccessException("User is not authenticated.");

    private static CopilotConversationSummary ToSummary(
        CopilotConversation conversation) =>
        new(
            conversation.Id,
            conversation.IncidentId,
            conversation.SituationId,
            conversation.Title,
            conversation.Summary,
            conversation.LastActivityAtUtc,
            conversation.ArchivedAtUtc);
}
