using EmergencyResponse.Application.Map;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/map")]
[Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
public sealed class MapController : ControllerBase
{
    private readonly IEmergencyMapService _mapService;

    public MapController(IEmergencyMapService mapService)
    {
        _mapService = mapService;
    }

    [HttpGet("incidents")]
    public async Task<ActionResult<IReadOnlyList<EmergencyMapIncident>>> GetIncidents(
        CancellationToken cancellationToken = default)
    {
        var incidents = await _mapService.GetIncidentsAsync(cancellationToken);
        return Ok(incidents);
    }
}
