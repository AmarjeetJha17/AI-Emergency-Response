using EmergencyResponse.Application.Escalation;
using EmergencyResponse.Application.Escalation.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/escalations")]
[Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
public sealed class EscalationsController : ControllerBase
{
    private readonly IIncidentEscalationService _escalationService;

    public EscalationsController(IIncidentEscalationService escalationService)
    {
        _escalationService = escalationService;
    }

    [HttpGet("open")]
    public async Task<ActionResult<IReadOnlyList<IncidentEscalationResponse>>> GetOpen(
        CancellationToken cancellationToken)
    {
        var result = await _escalationService.GetOpenEscalationsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("incident/{incidentId:guid}")]
    public async Task<ActionResult<IReadOnlyList<IncidentEscalationResponse>>> GetByIncident(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var result = await _escalationService.GetByIncidentIdAsync(incidentId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("evaluate/{incidentId:guid}")]
    public async Task<ActionResult<IReadOnlyList<IncidentEscalationResponse>>> Evaluate(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var result = await _escalationService.EvaluateIncidentAsync(incidentId, null, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{escalationId:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(
        Guid escalationId,
        [FromBody] EscalationActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _escalationService.AcknowledgeAsync(
                escalationId,
                request.Notes,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{escalationId:guid}/resolve")]
    public async Task<IActionResult> Resolve(
        Guid escalationId,
        [FromBody] EscalationActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _escalationService.ResolveAsync(
                escalationId,
                request.Notes,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("{escalationId:guid}/dismiss")]
    public async Task<IActionResult> Dismiss(
        Guid escalationId,
        [FromBody] EscalationActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _escalationService.DismissAsync(
                escalationId,
                request.Notes,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
