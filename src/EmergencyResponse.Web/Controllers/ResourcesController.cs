using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Web.Models.Resources;
using EmergencyResponse.Web.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Web.Controllers;

[Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
public sealed class ResourcesController : Controller
{
    private readonly IResourceApiClient _resourceApiClient;
    private readonly IIncidentApiClient _incidentApiClient;
    private readonly ILogger<ResourcesController> _logger;

    public ResourcesController(
        IResourceApiClient resourceApiClient,
        IIncidentApiClient incidentApiClient,
        ILogger<ResourcesController> logger)
    {
        _resourceApiClient = resourceApiClient;
        _incidentApiClient = incidentApiClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summaryTask = _resourceApiClient.GetSummaryAsync(cancellationToken);
        var resourcesTask = _resourceApiClient.GetCommandCenterAsync(cancellationToken);

        await Task.WhenAll(summaryTask, resourcesTask);

        var viewModel = new ResourceCommandCenterViewModel
        {
            Summary = await summaryTask,
            Resources = await resourcesTask,
            RetrievedAtUtc = DateTime.UtcNow
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Snapshot(CancellationToken cancellationToken)
    {
        var summaryTask = _resourceApiClient.GetSummaryAsync(cancellationToken);
        var resourcesTask = _resourceApiClient.GetCommandCenterAsync(cancellationToken);

        await Task.WhenAll(summaryTask, resourcesTask);

        return Json(new
        {
            summary = await summaryTask,
            resources = await resourcesTask,
            updatedAtUtc = DateTime.UtcNow
        });
    }

    [HttpGet]
    public async Task<IActionResult> AssignableIncidents(
        [FromQuery] Guid? resourceId,
        CancellationToken cancellationToken)
    {
        var incidents = await _resourceApiClient.GetAssignableIncidentsAsync(resourceId, cancellationToken);
        return Json(incidents);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(
        [FromBody] AssignResourceViewModel model,
        CancellationToken cancellationToken)
    {
        if (model == null || model.ResourceId == Guid.Empty || model.IncidentId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "Resource ID and Incident ID are required." });
        }

        try
        {
            var result = await _resourceApiClient.AssignAsync(
                model.ResourceId,
                new AssignResourceRequest
                {
                    IncidentId = model.IncidentId,
                    Notes = model.Notes
                },
                cancellationToken);

            return Json(new { success = true, result });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Resource assignment failed for resource {ResourceId}", model.ResourceId);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during resource assignment.");
            return StatusCode(500, new { success = false, message = "An error occurred while assigning the resource." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeStatus(
        [FromBody] ChangeStatusViewModel model,
        CancellationToken cancellationToken)
    {
        if (model == null || model.ResourceId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "Resource ID is required." });
        }

        try
        {
            var result = await _resourceApiClient.ChangeStatusAsync(
                model.ResourceId,
                new ChangeResourceStatusRequest
                {
                    Status = model.Status,
                    IncidentId = model.IncidentId,
                    Reason = model.Reason
                },
                cancellationToken);

            return Json(new { success = true, result });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Resource status change failed for resource {ResourceId}", model.ResourceId);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during resource status transition.");
            return StatusCode(500, new { success = false, message = "An error occurred while changing resource status." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(
        [FromBody] ReleaseResourceViewModel model,
        CancellationToken cancellationToken)
    {
        if (model == null || model.ResourceId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "Resource ID is required." });
        }

        try
        {
            await _resourceApiClient.ReleaseAsync(
                model.ResourceId,
                new ReleaseResourceRequest
                {
                    Notes = model.Notes
                },
                cancellationToken);

            return Json(new { success = true });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Resource release failed for resource {ResourceId}", model.ResourceId);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during resource release.");
            return StatusCode(500, new { success = false, message = "An error occurred while releasing the resource." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resource = await _resourceApiClient.GetByIdAsync(id, cancellationToken);
        if (resource == null)
        {
            return NotFound();
        }

        return View(resource);
    }

    [HttpGet]
    public async Task<IActionResult> Recommendations(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var incident = await _incidentApiClient.GetByIdAsync(incidentId, cancellationToken);
        if (incident is null)
        {
            return NotFound($"Incident {incidentId} not found.");
        }

        var recommendations = await _resourceApiClient.GetRecommendationsAsync(incidentId, cancellationToken);

        var viewModel = new ResourceRecommendationsViewModel
        {
            Incident = incident,
            Recommendation = recommendations
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> RecommendationsData(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var recommendations = await _resourceApiClient.GetRecommendationsAsync(incidentId, cancellationToken);
            return Json(recommendations);
        }
        catch (HttpRequestException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Command(
        [FromBody] ResourceCommandViewModel model,
        CancellationToken cancellationToken)
    {
        if (model == null || model.ResourceId == Guid.Empty)
        {
            return BadRequest(new { success = false, message = "Resource ID is required." });
        }

        try
        {
            var result = await _resourceApiClient.ExecuteCommandAsync(
                model.ResourceId,
                new ResourceCommandRequest
                {
                    Command = model.Command,
                    Notes = model.Notes
                },
                cancellationToken);

            return Json(new { success = true, result });
        }
        catch (ResourceConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Resource command concurrency conflict for resource {ResourceId}", model.ResourceId);
            return StatusCode(StatusCodes.Status409Conflict, new { success = false, message = ex.Message });
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            _logger.LogWarning(ex, "Resource command concurrency conflict for resource {ResourceId}", model.ResourceId);
            return StatusCode(StatusCodes.Status409Conflict, new { success = false, message = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Resource command failed for resource {ResourceId}", model.ResourceId);
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during resource command.");
            return StatusCode(500, new { success = false, message = "An error occurred while executing the resource command." });
        }
    }
}

public sealed class AssignResourceViewModel
{
    public Guid ResourceId { get; set; }
    public Guid IncidentId { get; set; }
    public string? Notes { get; set; }
}

public sealed class ChangeStatusViewModel
{
    public Guid ResourceId { get; set; }
    public ResourceStatus Status { get; set; }
    public Guid? IncidentId { get; set; }
    public string? Reason { get; set; }
}

public sealed class ReleaseResourceViewModel
{
    public Guid ResourceId { get; set; }
    public string? Notes { get; set; }
}

public sealed class ResourceCommandViewModel
{
    public Guid ResourceId { get; set; }
    public ResourceCommand Command { get; set; }
    public string? Notes { get; set; }
}
