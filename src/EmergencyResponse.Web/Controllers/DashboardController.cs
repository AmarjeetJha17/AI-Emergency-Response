using EmergencyResponse.Application.Dashboard;
using EmergencyResponse.Web.Models.Dashboard;
using EmergencyResponse.Web.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Web.Controllers;

[Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
public sealed class DashboardController : Controller
{
    private readonly IDashboardApiClient _dashboardApiClient;

    public DashboardController(IDashboardApiClient dashboardApiClient)
    {
        _dashboardApiClient = dashboardApiClient;
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> IncidentVolume(CancellationToken cancellationToken) =>
        GetForecastAsync(() => _dashboardApiClient.GetIncidentVolumeAsync(cancellationToken), cancellationToken);

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> Hotspots(CancellationToken cancellationToken) =>
        GetForecastAsync(() => _dashboardApiClient.GetHotspotsAsync(cancellationToken), cancellationToken);

    // Reuse the existing bearer-token HTTP client; never send an API token to page scripts.
    private async Task<IActionResult> GetForecastAsync<T>(Func<Task<T>> request, CancellationToken cancellationToken)
    {
        try { return Json(await request()); }
        catch (HttpRequestException error) when (error.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        { return StatusCode((int)error.StatusCode.Value); }
        catch (Exception error) when (error is HttpRequestException or System.Text.Json.JsonException ||
            error is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        { return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Forecast data is unavailable." }); }
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] Guid? incidentId,
        CancellationToken cancellationToken)
    {
        var summary = await _dashboardApiClient.GetSummaryAsync(
            cancellationToken);

        var incidents = await _dashboardApiClient.GetIncidentsAsync(
            100,
            cancellationToken);

        var severity = await _dashboardApiClient.GetSeverityAsync(
            cancellationToken);

        var trend = await _dashboardApiClient.GetTrendAsync(
            7,
            cancellationToken);

        var gridItems = incidents
            .Select(x => new DashboardIncidentGridItem
            {
                Id                     = x.Id,
                IncidentNumber         = x.IncidentNumber,
                Title                  = x.Title,
                IncidentType           = x.IncidentType,
                Severity               = x.Severity.ToString(),
                Priority               = x.Priority.ToString(),
                Status                 = x.Status.ToString(),
                CreatedAtUtc           = x.CreatedAtUtc,
                IsAssigned             = x.IsAssigned,
                RequiresAIReview       = x.RequiresAIReview,
                HasDuplicateCandidates = x.HasDuplicateCandidates,
                HasVisualReview        = x.HasVisualReview,
                DetailsUrl             = Url.Action(
                                             "Details",
                                             "Incidents",
                                             new { id = x.Id })!
            })
            .ToList();

        var reviewQueue = new[]
        {
            new DashboardReviewPoint
            {
                Category = "AI Review",
                Count = summary.AwaitingAIReview
            },
            new DashboardReviewPoint
            {
                Category = "Duplicate Review",
                Count = summary.AwaitingDuplicateReview
            },
            new DashboardReviewPoint
            {
                Category = "Visual Review",
                Count = summary.AwaitingVisualReview
            }
        };

        return View(new DashboardViewModel
        {
            Summary              = summary,
            Incidents            = incidents,
            SeverityDistribution = severity,
            IncidentTrend        = trend,
            GridItems            = gridItems,
            ReviewQueue          = reviewQueue,
            SelectedIncidentId   = incidentId
        });
    }

    [HttpGet]
    public async Task<IActionResult> Snapshot(
        CancellationToken cancellationToken)
    {
        var summary = await _dashboardApiClient.GetSummaryAsync(
            cancellationToken);

        var incidents = await _dashboardApiClient.GetIncidentsAsync(
            100,
            cancellationToken);

        var severity = await _dashboardApiClient.GetSeverityAsync(
            cancellationToken);

        var trend = await _dashboardApiClient.GetTrendAsync(
            7,
            cancellationToken);

        var gridItems = incidents
            .Select(x => new DashboardIncidentGridItem
            {
                Id                     = x.Id,
                IncidentNumber         = x.IncidentNumber,
                Title                  = x.Title,
                IncidentType           = x.IncidentType,
                Severity               = x.Severity.ToString(),
                Priority               = x.Priority.ToString(),
                Status                 = x.Status.ToString(),
                CreatedAtUtc           = x.CreatedAtUtc,
                IsAssigned             = x.IsAssigned,
                RequiresAIReview       = x.RequiresAIReview,
                HasDuplicateCandidates = x.HasDuplicateCandidates,
                HasVisualReview        = x.HasVisualReview,
                DetailsUrl             = Url.Action(
                                             "Details",
                                             "Incidents",
                                             new { id = x.Id })!
            })
            .ToList();

        var reviewQueue = new[]
        {
            new DashboardReviewPoint
            {
                Category = "AI Review",
                Count = summary.AwaitingAIReview
            },
            new DashboardReviewPoint
            {
                Category = "Duplicate Review",
                Count = summary.AwaitingDuplicateReview
            },
            new DashboardReviewPoint
            {
                Category = "Visual Review",
                Count = summary.AwaitingVisualReview
            }
        };

        return Json(new
        {
            summary,
            incidents = gridItems,
            severityDistribution = severity,
            incidentTrend = trend,
            reviewQueue,
            updatedAtUtc = DateTime.UtcNow
        });
    }
}
