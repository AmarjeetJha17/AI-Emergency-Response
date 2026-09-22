using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Map;
using EmergencyResponse.Web.Models.Map;
using EmergencyResponse.Web.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Web.Controllers;

[Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
public sealed class MapController : Controller
{
    private readonly IEmergencyMapApiClient _mapApiClient;
    private readonly IIncidentApiClient _incidentApiClient;
    private readonly MapOptions _mapOptions;

    public MapController(
        IEmergencyMapApiClient mapApiClient,
        IIncidentApiClient incidentApiClient,
        IOptions<MapOptions> mapOptions)
    {
        _mapApiClient = mapApiClient;
        _incidentApiClient = incidentApiClient;
        _mapOptions = mapOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] Guid? incidentId,
        [FromQuery] Guid? resourceId,
        CancellationToken cancellationToken)
    {
        var incidentsTask = _mapApiClient.GetIncidentsAsync(cancellationToken);
        var resourcesTask = _mapApiClient.GetResourcesAsync(cancellationToken);
        var zonesTask = _mapApiClient.GetOperationalZonesAsync(cancellationToken);
        var typesTask = _mapApiClient.GetIncidentTypesAsync(cancellationToken);
        var deptsTask = _mapApiClient.GetDepartmentsAsync(cancellationToken);

        await Task.WhenAll(incidentsTask, resourcesTask, zonesTask, typesTask, deptsTask);

        var viewModel = new EmergencyMapViewModel
        {
            Incidents = await incidentsTask,
            Resources = await resourcesTask,
            OperationalZones = await zonesTask,
            IncidentTypes = await typesTask,
            Departments = await deptsTask,
            SelectedIncidentId = incidentId,
            SelectedResourceId = resourceId,
            TileUrl = _mapOptions.TileUrl,
            Attribution = _mapOptions.Attribution,
            DefaultLatitude = _mapOptions.DefaultLatitude,
            DefaultLongitude = _mapOptions.DefaultLongitude,
            DefaultZoom = _mapOptions.DefaultZoom
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Snapshot(CancellationToken cancellationToken)
    {
        var incidentsTask = _mapApiClient.GetIncidentsAsync(cancellationToken);
        var resourcesTask = _mapApiClient.GetResourcesAsync(cancellationToken);
        var zonesTask = _mapApiClient.GetOperationalZonesAsync(cancellationToken);

        await Task.WhenAll(incidentsTask, resourcesTask, zonesTask);

        return Json(new
        {
            incidents = await incidentsTask,
            resources = await resourcesTask,
            operationalZones = await zonesTask,
            updatedAtUtc = DateTime.UtcNow
        });
    }

    [HttpGet]
    public async Task<IActionResult> Incident(
        Guid id,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentApiClient.GetByIdAsync(
            id,
            cancellationToken);

        if (incident is null)
        {
            return NotFound();
        }

        return Json(incident);
    }
}
