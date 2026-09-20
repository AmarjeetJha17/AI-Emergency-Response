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
    private readonly MapOptions _mapOptions;

    public MapController(
        IEmergencyMapApiClient mapApiClient,
        IOptions<MapOptions> mapOptions)
    {
        _mapApiClient = mapApiClient;
        _mapOptions = mapOptions.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var incidents = await _mapApiClient.GetIncidentsAsync(cancellationToken);

        var viewModel = new EmergencyMapViewModel
        {
            Incidents = incidents,
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
        var incidents = await _mapApiClient.GetIncidentsAsync(cancellationToken);

        return Json(new
        {
            incidents,
            updatedAtUtc = DateTime.UtcNow
        });
    }
}
