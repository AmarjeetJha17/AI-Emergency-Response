using System.Text.Json;
using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Application.Incidents;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Observability;
using EmergencyResponse.Domain.Copilot;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Copilot.Actions;

public sealed class CopilotActionExecutor : ICopilotActionExecutor
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IIncidentAnnotationService _annotations;
    private readonly ICopilotActionNotificationService _notifications;

    public CopilotActionExecutor(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IIncidentAnnotationService annotations,
        ICopilotActionNotificationService notifications)
    {
        _db = db;
        _currentUser = currentUser;
        _annotations = annotations;
        _notifications = notifications;
    }

    public async Task<CopilotActionExecutionResult> ExecuteAsync(
        Guid proposalId,
        CancellationToken cancellationToken = default)
    {
        var proposal = await ClaimAsync(proposalId, cancellationToken);
        var executedAt = DateTime.UtcNow;

        try
        {
            var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                proposal.ArgumentsJson)
                ?? throw new InvalidOperationException("Action arguments are invalid.");
            if (proposal.IncidentId is not Guid incidentId)
            {
                throw new InvalidOperationException("The action has no incident target.");
            }

            IncidentAnnotationResult result = proposal.ActionType switch
            {
                CopilotActionType.CreateIncidentNote =>
                    await _annotations.AddNoteAsync(
                        incidentId,
                        GetString(arguments, "content"),
                        cancellationToken),
                CopilotActionType.AddIncidentTag =>
                    await _annotations.AddTagAsync(
                        incidentId,
                        GetString(arguments, "tag"),
                        cancellationToken),
                _ => throw new InvalidOperationException(
                    "The action type is not enabled for execution.")
            };

            proposal.Status = CopilotActionStatus.Succeeded;
            proposal.ExecutedAtUtc = executedAt;
            proposal.ExecutionResultJson = JsonSerializer.Serialize(result);
            AddAudit(proposal, "COPILOT_ACTION_SUCCEEDED", executedAt);
            await _db.SaveChangesAsync(cancellationToken);
            await _notifications.PublishAsync(
                new CopilotActionUpdatedEvent(
                    proposal.Id,
                    proposal.ActionType,
                    proposal.Status,
                    proposal.IncidentId,
                    executedAt),
                proposal.ProposedByUserId,
                cancellationToken);
            EmergencyResponseTelemetry.CopilotActionsExecuted.Add(
                1,
                new KeyValuePair<string, object?>("action_type", proposal.ActionType.ToString()));

            return new CopilotActionExecutionResult(
                true,
                "The approved Copilot action completed successfully.",
                null,
                proposal.ExecutionResultJson,
                executedAt);
        }
        catch (Exception ex) when (ex is ArgumentException or
                                   InvalidOperationException or
                                   KeyNotFoundException or
                                   DbUpdateException)
        {
            var safeReason = ex is DbUpdateException
                ? "The incident annotation could not be persisted."
                : ex.Message;
            proposal.Status = CopilotActionStatus.Failed;
            proposal.ExecutedAtUtc = executedAt;
            proposal.FailureReason = safeReason;
            proposal.ExecutionResultJson = JsonSerializer.Serialize(new
            {
                succeeded = false,
                error = safeReason,
                executedAtUtc = executedAt
            });
            AddAudit(proposal, "COPILOT_ACTION_FAILED", executedAt);
            await _db.SaveChangesAsync(cancellationToken);
            await _notifications.PublishAsync(
                new CopilotActionUpdatedEvent(
                    proposal.Id,
                    proposal.ActionType,
                    proposal.Status,
                    proposal.IncidentId,
                    executedAt),
                proposal.ProposedByUserId,
                cancellationToken);
            EmergencyResponseTelemetry.CopilotActionFailures.Add(
                1,
                new KeyValuePair<string, object?>("action_type", proposal.ActionType.ToString()));

            return new CopilotActionExecutionResult(
                false,
                "The approved Copilot action could not be completed.",
                "ACTION_EXECUTION_FAILED",
                proposal.ExecutionResultJson,
                executedAt);
        }
    }

    private async Task<CopilotActionProposal> ClaimAsync(
        Guid proposalId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);
        var proposal = await _db.CopilotActionProposals
            .SingleOrDefaultAsync(x => x.Id == proposalId, cancellationToken)
            ?? throw new KeyNotFoundException("The action proposal was not found.");

        if (proposal.ExpiresAtUtc is { } expiresAt &&
            expiresAt <= DateTime.UtcNow)
        {
            proposal.Status = CopilotActionStatus.Expired;
            proposal.ApprovalDecision = CopilotApprovalDecision.Expired;
            proposal.FailureReason = "The action proposal expired before execution.";
            AddAudit(proposal, "COPILOT_ACTION_EXPIRED", DateTime.UtcNow);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            throw new InvalidOperationException("The action proposal has expired.");
        }

        if (proposal.Status != CopilotActionStatus.Approved ||
            proposal.ApprovedByUserId is null)
        {
            throw new InvalidOperationException(
                "Only an approved action proposal can execute.");
        }

        if (proposal.ApprovedByUserId != userId)
        {
            throw new UnauthorizedAccessException(
                "Only the authenticated approver may execute this proposal.");
        }

        CopilotActionStateMachine.EnsureTransition(
            proposal.Status,
            CopilotActionStatus.Executing);
        proposal.Status = CopilotActionStatus.Executing;
        AddAudit(proposal, "COPILOT_ACTION_EXECUTING", DateTime.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return proposal;
    }

    private static string GetString(
        IReadOnlyDictionary<string, JsonElement> arguments,
        string name)
    {
        if (!arguments.TryGetValue(name, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            throw new ArgumentException($"The action argument '{name}' is required.");
        }

        var text = value.GetString()?.Trim() ?? string.Empty;
        if (text.Length == 0 || text.Length > 500)
        {
            throw new ArgumentException(
                $"The action argument '{name}' is invalid.");
        }

        return text;
    }

    private void AddAudit(
        CopilotActionProposal proposal,
        string action,
        DateTime occurredAtUtc)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUser.UserId,
            Action = action,
            EntityName = nameof(CopilotActionProposal),
            EntityId = proposal.Id.ToString(),
            NewValues = JsonSerializer.Serialize(new
            {
                proposal.ActionType,
                proposal.Status,
                proposal.IncidentId,
                proposal.CorrelationId,
                proposal.CopilotInteractionId,
                proposal.ExecutionResultJson,
                proposal.FailureReason
            }),
            OccurredAtUtc = occurredAtUtc
        });
    }
}
