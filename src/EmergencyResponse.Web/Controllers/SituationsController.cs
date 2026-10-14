using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Situations.DTOs;
using EmergencyResponse.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Web.Controllers;

[Authorize(Roles = $"{ApplicationRoles.Responder},{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
public sealed class SituationsController : Controller
{
    private readonly ISituationApiClient _situationApiClient;
    private readonly ILogger<SituationsController> _logger;

    public SituationsController(
        ISituationApiClient situationApiClient,
        ILogger<SituationsController> logger)
    {
        _situationApiClient = situationApiClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] string? level,
        [FromQuery] bool? resourceShortage,
        CancellationToken cancellationToken)
    {
        try
        {
            var situations = await _situationApiClient.GetCommandCenterAsync(cancellationToken);

            ViewBag.AllSituations = situations;
            ViewBag.ActiveLevelFilter = level;
            ViewBag.ActiveResourceShortageFilter = resourceShortage;

            var filtered = situations.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(level))
            {
                filtered = filtered.Where(s => s.Level.ToString().Equals(level, StringComparison.OrdinalIgnoreCase));
            }

            if (resourceShortage == true)
            {
                filtered = filtered.Where(s => s.ResourceShortageDetected);
            }

            return View(filtered.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load situation command center.");
            ViewBag.ErrorMessage = "Unable to load situations. Please ensure the API is running.";
            ViewBag.AllSituations = Array.Empty<SituationCommandCenterItem>();
            return View(Array.Empty<SituationCommandCenterItem>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Snapshot(CancellationToken cancellationToken)
    {
        try
        {
            var data = await _situationApiClient.GetCommandCenterAsync(cancellationToken);
            return Json(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load situations snapshot.");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> MapSnapshot(CancellationToken cancellationToken)
    {
        try
        {
            var data = await _situationApiClient.GetOpenForMapAsync(cancellationToken);
            return Json(data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load situation map snapshot.");
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var situation = await _situationApiClient.GetByIdAsync(id, cancellationToken);
            if (situation == null)
            {
                return NotFound();
            }

            return View(situation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load situation details for {SituationId}", id);
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Evaluate(CancellationToken cancellationToken)
    {
        try
        {
            await _situationApiClient.EvaluateAsync(cancellationToken);
            return Json(new { success = true, message = "Situation evaluation triggered." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger situation evaluation.");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Acknowledge(
        Guid id,
        [FromBody] SituationActionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _situationApiClient.AcknowledgeAsync(id, request?.Notes, cancellationToken);
            return Json(new { success = true, message = "Situation acknowledged successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge situation {SituationId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Review(
        Guid id,
        [FromBody] ReviewSituationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _situationApiClient.ReviewAsync(id, request, cancellationToken);
            return Json(new { success = true, message = "Situation reviewed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to review situation {SituationId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] SituationActionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _situationApiClient.ResolveAsync(id, request?.Notes, cancellationToken);
            return Json(new { success = true, message = "Situation resolved successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve situation {SituationId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Dismiss(
        Guid id,
        [FromBody] SituationActionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _situationApiClient.DismissAsync(id, request?.Notes, cancellationToken);
            return Json(new { success = true, message = "Situation dismissed successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dismiss situation {SituationId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
