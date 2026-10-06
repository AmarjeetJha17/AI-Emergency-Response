using System.Text.Json;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Observability;
using EmergencyResponse.Domain.Copilot;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Copilot.Actions;

public sealed class CopilotActionService : ICopilotActionService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ICopilotActionPolicy _policy;
    private readonly ICopilotActionNotificationService _notifications;

    public CopilotActionService(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        ICopilotActionPolicy policy,
        ICopilotActionNotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _policy = policy;
        _notifications = notifications;
    }

    public async Task<CopilotActionProposal> ProposeAsync(
        Guid copilotInteractionId,
        CopilotActionProposalRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = RequireUser();
        var roles = GetRoles();
        var now = DateTime.UtcNow;
        var decision = _policy.Evaluate(request.ActionType, roles, now);
        if (!decision.Allowed)
        {
            throw new UnauthorizedAccessException(
                decision.DenialReason ?? "The action is not authorized.");
        }

        if (request.IncidentId is null)
        {
            throw new ArgumentException(
                "An incident is required for this action.",
                nameof(request));
        }

        var incidentExists = await _db.Incidents
            .AsNoTracking()
            .AnyAsync(x => x.Id == request.IncidentId, cancellationToken);
        if (!incidentExists)
        {
            throw new KeyNotFoundException("The incident was not found.");
        }

        var title = RequireBounded(request.Title, 300, nameof(request.Title));
        var description = RequireBounded(
            request.Description,
            2000,
            nameof(request.Description));
        var proposal = new CopilotActionProposal
        {
            Id = Guid.NewGuid(),
            CopilotInteractionId = copilotInteractionId,
            ActionType = request.ActionType,
            RiskLevel = decision.RiskLevel,
            Status = CopilotActionStatus.AwaitingApproval,
            Title = title,
            Description = description,
            ArgumentsJson = JsonSerializer.Serialize(request.Arguments),
            IncidentId = request.IncidentId,
            SituationId = request.SituationId,
            ProposedByUserId = userId,
            ProposedAtUtc = now,
            ExpiresAtUtc = decision.ExpiresAtUtc,
            ApprovalDecision = CopilotApprovalDecision.Pending,
            PolicySnapshotJson = JsonSerializer.Serialize(new
            {
                decision.PolicyVersion,
                decision.RequiredRoles,
                decision.RequiresApproval,
                decision.RiskLevel,
                decision.ExpiresAtUtc
            }),
            CorrelationId = Guid.NewGuid().ToString("N")
        };

        _db.CopilotActionProposals.Add(proposal);
        AddAudit(proposal, "COPILOT_ACTION_PROPOSED", userId, now);
        await _db.SaveChangesAsync(cancellationToken);
        await _notifications.PublishAsync(
            new CopilotActionUpdatedEvent(
                proposal.Id,
                proposal.ActionType,
                proposal.Status,
                proposal.IncidentId,
                now),
            proposal.ProposedByUserId,
            cancellationToken);
        EmergencyResponseTelemetry.CopilotActionsProposed.Add(
            1,
            new KeyValuePair<string, object?>("action_type", proposal.ActionType.ToString()));
        return proposal;
    }

    public async Task<CopilotActionProposal> ApproveAsync(
        Guid proposalId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var proposal = await GetRequiredAsync(proposalId, cancellationToken);
        var userId = RequireUser();
        await EnsureNotExpiredAsync(proposal, cancellationToken);
        EnsureCurrentUserCanApprove(proposal.ActionType);
        CopilotActionStateMachine.EnsureTransition(
            proposal.Status,
            CopilotActionStatus.Approved);

        proposal.Status = CopilotActionStatus.Approved;
        proposal.ApprovalDecision = CopilotApprovalDecision.Approved;
        proposal.ApprovedByUserId = userId;
        proposal.ApprovedAtUtc = DateTime.UtcNow;
        proposal.ApprovalComment = comment?.Trim()[..Math.Min(1000, comment.Trim().Length)];
        AddAudit(proposal, "COPILOT_ACTION_APPROVED", userId, proposal.ApprovedAtUtc.Value);
        await _db.SaveChangesAsync(cancellationToken);
        await _notifications.PublishAsync(
            new CopilotActionUpdatedEvent(
                proposal.Id,
                proposal.ActionType,
                proposal.Status,
                proposal.IncidentId,
                proposal.ApprovedAtUtc.Value),
            proposal.ProposedByUserId,
            cancellationToken);
        EmergencyResponseTelemetry.CopilotActionsApproved.Add(
            1,
            new KeyValuePair<string, object?>("action_type", proposal.ActionType.ToString()));
        return proposal;
    }

    public async Task<CopilotActionProposal> RejectAsync(
        Guid proposalId,
        string? comment,
        CancellationToken cancellationToken = default)
    {
        var proposal = await GetRequiredAsync(proposalId, cancellationToken);
        var userId = RequireUser();
        await EnsureNotExpiredAsync(proposal, cancellationToken);
        EnsureCurrentUserCanApprove(proposal.ActionType);
        CopilotActionStateMachine.EnsureTransition(
            proposal.Status,
            CopilotActionStatus.Rejected);

        proposal.Status = CopilotActionStatus.Rejected;
        proposal.ApprovalDecision = CopilotApprovalDecision.Rejected;
        proposal.ApprovedByUserId = userId;
        proposal.ApprovedAtUtc = DateTime.UtcNow;
        proposal.ApprovalComment = comment?.Trim()[..Math.Min(1000, comment.Trim().Length)];
        AddAudit(proposal, "COPILOT_ACTION_REJECTED", userId, proposal.ApprovedAtUtc.Value);
        await _db.SaveChangesAsync(cancellationToken);
        await _notifications.PublishAsync(
            new CopilotActionUpdatedEvent(
                proposal.Id,
                proposal.ActionType,
                proposal.Status,
                proposal.IncidentId,
                proposal.ApprovedAtUtc.Value),
            proposal.ProposedByUserId,
            cancellationToken);
        EmergencyResponseTelemetry.CopilotActionsRejected.Add(
            1,
            new KeyValuePair<string, object?>("action_type", proposal.ActionType.ToString()));
        return proposal;
    }

    public Task<CopilotActionProposal?> GetAsync(
        Guid proposalId,
        CancellationToken cancellationToken = default) =>
        GetVisibleAsync(proposalId, cancellationToken);

    public async Task<IReadOnlyList<CopilotActionProposal>> GetPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = GetRoles();
        if (!_policy.Evaluate(
                CopilotActionType.CreateIncidentNote,
                roles,
                DateTime.UtcNow).Allowed)
        {
            return [];
        }

        return await _db.CopilotActionProposals
            .AsNoTracking()
            .Where(x => x.Status == CopilotActionStatus.AwaitingApproval &&
                        x.ExpiresAtUtc > DateTime.UtcNow)
            .OrderBy(x => x.ExpiresAtUtc)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    private async Task<CopilotActionProposal?> GetVisibleAsync(
        Guid proposalId,
        CancellationToken cancellationToken)
    {
        var userId = RequireUser();
        var roles = GetRoles();
        var canApprove = roles.Any(role =>
            role is ApplicationRoles.Operator or
            ApplicationRoles.EmergencyAdmin or
            ApplicationRoles.SystemAdmin);

        return await _db.CopilotActionProposals
            .AsNoTracking()
            .Where(x => x.Id == proposalId &&
                        (x.ProposedByUserId == userId ||
                         (x.Status == CopilotActionStatus.AwaitingApproval &&
                          canApprove)))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<CopilotActionProposal> GetRequiredAsync(
        Guid proposalId,
        CancellationToken cancellationToken) =>
        await _db.CopilotActionProposals
            .FirstOrDefaultAsync(x => x.Id == proposalId, cancellationToken)
        ?? throw new KeyNotFoundException("The action proposal was not found.");

    private async Task EnsureNotExpiredAsync(
        CopilotActionProposal proposal,
        CancellationToken cancellationToken)
    {
        if (proposal.ExpiresAtUtc is { } expiresAt &&
            expiresAt <= DateTime.UtcNow)
        {
            if (proposal.Status == CopilotActionStatus.AwaitingApproval)
            {
                proposal.Status = CopilotActionStatus.Expired;
                proposal.ApprovalDecision = CopilotApprovalDecision.Expired;
                AddAudit(
                    proposal,
                    "COPILOT_ACTION_EXPIRED",
                    RequireUser(),
                    DateTime.UtcNow);
                await _db.SaveChangesAsync(cancellationToken);
                EmergencyResponseTelemetry.CopilotActionsExpired.Add(
                    1,
                    new KeyValuePair<string, object?>("action_type", proposal.ActionType.ToString()));
            }

            throw new InvalidOperationException("The action proposal has expired.");
        }
    }

    private void EnsureCurrentUserCanApprove(CopilotActionType actionType)
    {
        var decision = _policy.Evaluate(
            actionType,
            GetRoles(),
            DateTime.UtcNow);
        if (!decision.Allowed)
        {
            throw new UnauthorizedAccessException(
                decision.DenialReason ?? "The current user cannot approve this action.");
        }
    }

    private string RequireUser() =>
        _currentUser.UserId
        ?? throw new UnauthorizedAccessException("User is not authenticated.");

    private IReadOnlyCollection<string> GetRoles() =>
    new[]
    {
        ApplicationRoles.Responder,
        ApplicationRoles.Operator,
        ApplicationRoles.EmergencyAdmin,
        ApplicationRoles.SystemAdmin
    }.Where(_currentUser.IsInRole).ToArray();

    private static string RequireBounded(
        string value,
        int maxLength,
        string parameterName)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0 || trimmed.Length > maxLength)
        {
            throw new ArgumentException(
                $"The value must contain between 1 and {maxLength} characters.",
                parameterName);
        }

        return trimmed;
    }

    private void AddAudit(
        CopilotActionProposal proposal,
        string action,
        string userId,
        DateTime occurredAtUtc)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityName = nameof(CopilotActionProposal),
            EntityId = proposal.Id.ToString(),
            NewValues = JsonSerializer.Serialize(new
            {
                proposal.ActionType,
                proposal.Status,
                proposal.IncidentId,
                proposal.CorrelationId,
                proposal.CopilotInteractionId
            }),
            OccurredAtUtc = occurredAtUtc
        });
    }
}
