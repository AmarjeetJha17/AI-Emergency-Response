using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Security.Authorization;
using EmergencyResponse.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/copilot/actions")]
[Authorize(Policy = Permissions.CopilotActionsPropose)]
public sealed class CopilotActionsController : ControllerBase
{
    private readonly ICopilotActionService _actions;
    private readonly ICopilotActionExecutor _executor;
    private readonly ICurrentUserService _currentUser;

    public CopilotActionsController(
        ICopilotActionService actions,
        ICopilotActionExecutor executor,
        ICurrentUserService currentUser)
    {
        _actions = actions;
        _executor = executor;
        _currentUser = currentUser;
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CopilotActionProposalView>> Get(
        Guid id,
        CancellationToken cancellationToken)
    {
        var proposal = await _actions.GetAsync(id, cancellationToken);
        return proposal is null ? NotFound() : Ok(ToView(proposal));
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IReadOnlyList<CopilotActionProposalView>>> Pending(
        CancellationToken cancellationToken) =>
        Ok((await _actions.GetPendingAsync(cancellationToken))
            .Select(ToView)
            .ToList());

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Permissions.CopilotActionsApprove)]
    [EnableRateLimiting("copilot")]
    public async Task<ActionResult<CopilotActionExecutionResult>> Approve(
        Guid id,
        ApproveCopilotActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _actions.ApproveAsync(id, request.Comment, cancellationToken);
            return Ok(await _executor.ExecuteAsync(id, cancellationToken));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("{id:guid}/reject")]
    [EnableRateLimiting("copilot")]
    public async Task<ActionResult<CopilotActionProposalView>> Reject(
        Guid id,
        RejectCopilotActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(ToView(await _actions.RejectAsync(
                id,
                request.Comment,
                cancellationToken)));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    private static CopilotActionProposalView ToView(
        CopilotActionProposal proposal) =>
        new(
            proposal.Id,
            proposal.ActionType,
            proposal.RiskLevel,
            proposal.Status,
            proposal.Title,
            proposal.Description,
            proposal.IncidentId,
            proposal.ProposedAtUtc,
            proposal.ExpiresAtUtc,
            proposal.ApprovalDecision,
            proposal.ApprovedByUserId,
            proposal.ApprovedAtUtc,
            proposal.ApprovalComment,
            proposal.ExecutedAtUtc,
            proposal.FailureReason,
            proposal.CorrelationId);
}
