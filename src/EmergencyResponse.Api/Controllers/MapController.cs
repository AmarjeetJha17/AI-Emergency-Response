using EmergencyResponse.Application.Map;
using EmergencyResponse.Application.Predictive.Hotspots;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/map")]
[Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
public sealed class MapController : ControllerBase
{
    private readonly IEmergencyMapService _mapService;
    private readonly IHotspotService _hotspotService;

    public MapController(
        IEmergencyMapService mapService,
        IHotspotService hotspotService)
    {
        _mapService = mapService;
        _hotspotService = hotspotService;
    }

    [HttpGet("incidents")]
    public async Task<ActionResult<IReadOnlyList<EmergencyMapIncident>>> GetIncidents(
        CancellationToken cancellationToken = default)
    {
        var incidents = await _mapService.GetIncidentsAsync(cancellationToken);
        return Ok(incidents);
    }

    [HttpGet("incident-types")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetIncidentTypes(
        CancellationToken cancellationToken = default)
    {
        var types = await _mapService.GetIncidentTypesAsync(cancellationToken);
        return Ok(types);
    }

    [HttpGet("departments")]
    public async Task<ActionResult<IReadOnlyList<string>>> GetDepartments(
        CancellationToken cancellationToken = default)
    {
        var departments = await _mapService.GetDepartmentsAsync(cancellationToken);
        return Ok(departments);
    }

    [HttpGet("resources")]
    public async Task<ActionResult<IReadOnlyList<MapResource>>> GetResources(
        CancellationToken cancellationToken = default)
    {
        var resources = await _mapService.GetResourcesAsync(cancellationToken);
        return Ok(resources);
    }

    [HttpGet("zones")]
    public async Task<ActionResult<IReadOnlyList<MapOperationalZone>>> GetOperationalZones(
        CancellationToken cancellationToken = default)
    {
        var zones = await _mapService.GetOperationalZonesAsync(cancellationToken);
        return Ok(zones);
    }

    [HttpGet("hotspots")]
    public async Task<ActionResult<IReadOnlyList<HotspotResult>>> GetHotspots(
        [FromQuery] int hours = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _hotspotService.GetHotspotsAsync(hours, cancellationToken));
    }
}
