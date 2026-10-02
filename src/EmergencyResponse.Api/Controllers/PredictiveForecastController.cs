using System.Text.Json;
using EmergencyResponse.Application.Predictive.Forecasting;
using EmergencyResponse.Application.Predictive.Hotspots;
using EmergencyResponse.Application.Predictive.ResourceDemand;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Predictive;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/predictive")]
[Authorize(Roles = "Responder,Operator,EmergencyAdmin,SystemAdmin")]
public sealed class PredictiveForecastController : ControllerBase
{
    private readonly IForecastingService _forecastingService;
    private readonly IResourceDemandForecastService _resourceDemandService;
    private readonly IHotspotService _hotspotService;
    private readonly ApplicationDbContext _db;

    public PredictiveForecastController(
        IForecastingService forecastingService,
        IResourceDemandForecastService resourceDemandService,
        IHotspotService hotspotService,
        ApplicationDbContext db)
    {
        _forecastingService = forecastingService;
        _resourceDemandService = resourceDemandService;
        _hotspotService = hotspotService;
        _db = db;
    }

    [HttpGet("incident-volume")]
    public async Task<ActionResult<ForecastResult>> GetIncidentVolume(
        [FromQuery] int horizon = 24,
        CancellationToken cancellationToken = default)
    {
        var result = await _forecastingService.ForecastAsync(
            ForecastType.IncidentVolume,
            "INCIDENT_VOLUME:ALL",
            ForecastGranularity.Hour,
            horizon,
            cancellationToken);
        await PersistAsync(result, cancellationToken);
        return Ok(result);
    }

    [HttpGet("resource-demand")]
    public async Task<ActionResult<ResourceDemandForecast>> GetResourceDemand(
        [FromQuery] string resourceType,
        [FromQuery] int horizon = 24,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            return BadRequest("resourceType is required.");
        }

        var result = await _resourceDemandService.ForecastAsync(
            resourceType,
            horizon,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("hotspots")]
    public async Task<ActionResult<IReadOnlyList<HotspotResult>>> GetHotspots(
        [FromQuery] int hours = 24,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _hotspotService.GetHotspotsAsync(hours, cancellationToken));
    }

    [HttpPost("regenerate")]
    public async Task<ActionResult<object>> Regenerate(
        [FromQuery] int horizon = 24,
        CancellationToken cancellationToken = default)
    {
        var volume = await _forecastingService.ForecastAsync(
            ForecastType.IncidentVolume,
            "INCIDENT_VOLUME:ALL",
            ForecastGranularity.Hour,
            horizon,
            cancellationToken);
        await PersistAsync(volume, cancellationToken);

        return Ok(new
        {
            IncidentVolume = volume,
            GeneratedAtUtc = DateTime.UtcNow
        });
    }

    private async Task PersistAsync(
        ForecastResult result,
        CancellationToken cancellationToken)
    {
        if (!result.IsAvailable || result.Points.Count == 0)
        {
            return;
        }

        _db.OperationalForecasts.Add(new OperationalForecast
        {
            Id = Guid.NewGuid(),
            ForecastType = result.ForecastType,
            Granularity = result.Granularity,
            SeriesKey = result.SeriesKey,
            ModelName = result.ModelName,
            ModelVersion = result.ModelVersion,
            FeatureSchemaVersion = "forecast-features-v1",
            ForecastGeneratedAtUtc = result.GeneratedAtUtc,
            ForecastStartAtUtc = result.Points[0].TimestampUtc,
            ForecastEndAtUtc = result.Points[^1].TimestampUtc,
            Horizon = result.Points.Count,
            ForecastJson = JsonSerializer.Serialize(result.Points.Select(x => x.Forecast)),
            LowerBoundJson = JsonSerializer.Serialize(result.Points.Select(x => x.LowerBound)),
            UpperBoundJson = JsonSerializer.Serialize(result.Points.Select(x => x.UpperBound)),
            MAE = result.MAE,
            RMSE = result.RMSE
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
