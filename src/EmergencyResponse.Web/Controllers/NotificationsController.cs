using EmergencyResponse.Application.Notifications.DTOs;
using EmergencyResponse.Domain.Notifications;
using EmergencyResponse.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmergencyResponse.Web.Controllers;

[Authorize]
public sealed class NotificationsController : Controller
{
    private readonly INotificationApiClient _notificationApiClient;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationApiClient notificationApiClient,
        ILogger<NotificationsController> logger)
    {
        _notificationApiClient = notificationApiClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? priority = null,
        string? status = null,
        string? type = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _notificationApiClient.GetForCurrentUserAsync(100, cancellationToken);

        if (!string.IsNullOrWhiteSpace(priority) && Enum.TryParse<NotificationPriority>(priority, true, out var pEnum))
        {
            items = items.Where(x => x.Priority == pEnum).ToList();
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("Unread", StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(x => !x.IsRead).ToList();
            }
            else if (status.Equals("Read", StringComparison.OrdinalIgnoreCase))
            {
                items = items.Where(x => x.IsRead).ToList();
            }
        }

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<NotificationType>(type, true, out var tEnum))
        {
            items = items.Where(x => x.Type == tEnum).ToList();
        }

        var summary = await _notificationApiClient.GetSummaryAsync(cancellationToken);

        ViewBag.Summary = summary;
        ViewBag.SelectedPriority = priority;
        ViewBag.SelectedStatus = status;
        ViewBag.SelectedType = type;

        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> Summary(CancellationToken cancellationToken)
    {
        var summary = await _notificationApiClient.GetSummaryAsync(cancellationToken);
        return Json(summary ?? new NotificationSummary());
    }

    [HttpGet]
    public async Task<IActionResult> Snapshot(
        [FromQuery] int take = 10,
        CancellationToken cancellationToken = default)
    {
        var items = await _notificationApiClient.GetForCurrentUserAsync(take, cancellationToken);
        return Json(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _notificationApiClient.MarkAsReadAsync(id, cancellationToken);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark notification {Id} as read", id);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        try
        {
            await _notificationApiClient.MarkAllAsReadAsync(cancellationToken);
            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark all notifications as read");
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}
