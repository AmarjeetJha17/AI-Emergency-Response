using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(
        IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<IncidentResponse>> Create(
        CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _incidentService.CreateAsync(
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IncidentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _incidentService.GetByIdAsync(
                id,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResult<IncidentListItem>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? incidentTypeId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        CancellationToken cancellationToken = default)
    {
        var result =
            await _incidentService.GetPagedAsync(
                page,
                pageSize,
                search,
                incidentTypeId,
                status,
                severity,
                cancellationToken);

        return Ok(result);
    }
}