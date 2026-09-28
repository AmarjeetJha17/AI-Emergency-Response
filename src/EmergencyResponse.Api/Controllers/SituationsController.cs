using EmergencyResponse.Api.Common;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Situations;
using EmergencyResponse.Application.Situations.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/situations")]
[Authorize(Roles = $"{ApplicationRoles.Responder},{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
public sealed class SituationsController : ControllerBase
{
    private readonly IOperationalSituationService _situationService;

    public SituationsController(IOperationalSituationService situationService)
    {
        _situationService = situationService;
    }

    [HttpGet("open")]
    public async Task<ActionResult<IReadOnlyList<OperationalSituationResponse>>> GetOpen(
        CancellationToken cancellationToken)
    {
        var result = await _situationService.GetOpenAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("command-center")]
    public async Task<ActionResult<IReadOnlyList<SituationCommandCenterItem>>> GetCommandCenter(
        CancellationToken cancellationToken)
    {
        var situations = await _situationService.GetCommandCenterAsync(cancellationToken);
        return Ok(situations);
    }

    [HttpGet("open/map")]
    public async Task<ActionResult<IReadOnlyList<SituationMapItem>>> GetOpenMap(
        CancellationToken cancellationToken)
    {
        var situations = await _situationService.GetOpenForMapAsync(cancellationToken);
        return Ok(situations);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OperationalSituationResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _situationService.GetByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFound($"Situation {id} not found.");
        }

        return Ok(result);
    }

    [HttpPost("evaluate")]
    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Evaluate(CancellationToken cancellationToken)
    {
        await _situationService.EvaluateAsync(cancellationToken);
        return Ok(new { message = "Situation evaluation triggered." });
    }

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Review(
        Guid id,
        [FromBody] ReviewSituationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _situationService.ReviewAsync(id, request, cancellationToken);
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

    [HttpPost("{id:guid}/acknowledge")]
    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Acknowledge(
        Guid id,
        [FromBody] SituationActionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _situationService.AcknowledgeAsync(id, request?.Notes, cancellationToken);
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

    [HttpPost("{id:guid}/resolve")]
    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] SituationActionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _situationService.ResolveAsync(id, request?.Notes, cancellationToken);
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

    [HttpPost("{id:guid}/dismiss")]
    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Dismiss(
        Guid id,
        [FromBody] SituationActionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _situationService.DismissAsync(id, request?.Notes, cancellationToken);
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

    [HttpPost("{id:guid}/reanalyze")]
    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Reanalyze(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _situationService.ReanalyzeAsync(id, cancellationToken);
            return Ok(new { message = $"Situation {id} reanalyzed successfully." });
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
