using EmergencyResponse.Application.Escalation.DTOs;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Web.Controllers;

[Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
public sealed class EscalationsController : Controller
{
    private readonly IEscalationApiClient _escalationApiClient;
    private readonly ILogger<EscalationsController> _logger;

    public EscalationsController(
        IEscalationApiClient escalationApiClient,
        ILogger<EscalationsController> logger)
    {
        _escalationApiClient = escalationApiClient;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Acknowledge(
        Guid id,
        [FromBody] EscalationActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _escalationApiClient.AcknowledgeAsync(id, request.Notes, cancellationToken);
            return Json(new { success = true, message = "Escalation acknowledged." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge escalation {EscalationId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = $"{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    public async Task<IActionResult> Resolve(
        Guid id,
        [FromBody] EscalationActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _escalationApiClient.ResolveAsync(id, request.Notes, cancellationToken);
            return Json(new { success = true, message = "Escalation resolved." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve escalation {EscalationId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dismiss(
        Guid id,
        [FromBody] EscalationActionRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _escalationApiClient.DismissAsync(id, request.Notes, cancellationToken);
            return Json(new { success = true, message = "Escalation dismissed." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dismiss escalation {EscalationId}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }
}
