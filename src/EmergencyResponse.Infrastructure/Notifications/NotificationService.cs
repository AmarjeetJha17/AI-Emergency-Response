using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Notifications;
using EmergencyResponse.Application.Notifications.DTOs;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Notifications;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.Notifications;

public sealed class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIncidentNotificationService _notificationDispatchService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ApplicationDbContext context,
        ICurrentUserService currentUserService,
        IIncidentNotificationService notificationDispatchService,
        ILogger<NotificationService> logger)
    {
        _context = context;
        _currentUserService = currentUserService;
        _notificationDispatchService = notificationDispatchService;
        _logger = logger;
    }

    public async Task<NotificationItem?> CreateAsync(
        NotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Anti-flooding deduplication: if an equivalent unread, non-expired notification exists, suppress.
        if (!string.IsNullOrWhiteSpace(request.DeduplicationKey))
        {
            var now = DateTime.UtcNow;
            var exists = await _context.Notifications.AnyAsync(n =>
                n.DeduplicationKey == request.DeduplicationKey &&
                !n.IsRead &&
                n.Status == NotificationStatus.Unread &&
                (n.ExpiresAtUtc == null || n.ExpiresAtUtc > now),
                cancellationToken);

            if (exists)
            {
                _logger.LogInformation(
                    "Notification with deduplication key {Key} suppressed because unread instance already exists",
                    request.DeduplicationKey);
                return null;
            }
        }

        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            TargetRole = request.TargetRole,
            TargetDepartmentId = request.TargetDepartmentId,
            Type = request.Type,
            Priority = request.Priority,
            Status = NotificationStatus.Unread,
            Title = request.Title,
            Message = request.Message,
            IncidentId = request.IncidentId,
            OperationalSituationId = request.OperationalSituationId,
            IncidentEscalationId = request.IncidentEscalationId,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = request.ExpiresAtUtc,
            DeduplicationKey = request.DeduplicationKey,
            MetadataJson = request.MetadataJson
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);

        // SignalR delivery (authoritative DB write completed)
        try
        {
            await _notificationDispatchService.NotifyNotificationCreatedAsync(
                notification,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast notification {Id} via SignalR", notification.Id);
        }

        return new NotificationItem
        {
            Id = notification.Id,
            Type = notification.Type,
            Priority = notification.Priority,
            Status = notification.Status,
            Title = notification.Title,
            Message = notification.Message,
            IncidentId = notification.IncidentId,
            OperationalSituationId = notification.OperationalSituationId,
            IncidentEscalationId = notification.IncidentEscalationId,
            IsRead = notification.IsRead,
            CreatedAtUtc = notification.CreatedAtUtc,
            ReadAtUtc = notification.ReadAtUtc,
            TargetRole = notification.TargetRole,
            ActionUrl = ResolveActionUrl(notification.IncidentId, notification.OperationalSituationId)
        };
    }

    public async Task<IReadOnlyList<NotificationItem>> GetForCurrentUserAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        take = Math.Clamp(take, 1, 100);
        var query = GetPermittedNotificationsQuery();

        var items = await query
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(take)
            .Select(n => new NotificationItem
            {
                Id = n.Id,
                Type = n.Type,
                Priority = n.Priority,
                Status = n.Status,
                Title = n.Title,
                Message = n.Message,
                IncidentId = n.IncidentId,
                IncidentNumber = n.Incident != null ? n.Incident.IncidentNumber : null,
                OperationalSituationId = n.OperationalSituationId,
                SituationNumber = n.OperationalSituation != null ? n.OperationalSituation.SituationNumber : null,
                IncidentEscalationId = n.IncidentEscalationId,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc,
                ReadAtUtc = n.ReadAtUtc,
                TargetRole = n.TargetRole,
                ActionUrl = n.IncidentId != null ? $"/Incidents/Details/{n.IncidentId}" :
                            n.OperationalSituationId != null ? $"/Situations/Details/{n.OperationalSituationId}" :
                            "/Dashboard"
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<NotificationSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var query = GetPermittedNotificationsQuery()
            .Where(n => !n.IsRead && n.Status == NotificationStatus.Unread);

        var total = await query.CountAsync(cancellationToken);
        var important = await query.CountAsync(n => n.Priority == NotificationPriority.Important, cancellationToken);
        var urgent = await query.CountAsync(n => n.Priority == NotificationPriority.Urgent, cancellationToken);
        var critical = await query.CountAsync(n => n.Priority == NotificationPriority.Critical, cancellationToken);

        return new NotificationSummary
        {
            TotalUnread = total,
            ImportantUnread = important,
            UrgentUnread = urgent,
            CriticalUnread = critical
        };
    }

    public async Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var notification = await _context.Notifications.FirstOrDefaultAsync(
            n => n.Id == notificationId,
            cancellationToken);

        if (notification == null)
        {
            throw new KeyNotFoundException($"Notification {notificationId} not found.");
        }

        if (!IsNotificationVisibleToCurrentUser(notification))
        {
            throw new UnauthorizedAccessException("User is not authorized to modify this notification.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.Status = NotificationStatus.Read;
            notification.ReadAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task MarkAllAsReadAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var query = GetPermittedNotificationsQuery()
            .Where(n => !n.IsRead && n.Status == NotificationStatus.Unread);

        var unreadNotifications = await query.ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var n in unreadNotifications)
        {
            n.IsRead = true;
            n.Status = NotificationStatus.Read;
            n.ReadAtUtc = now;
        }

        if (unreadNotifications.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private IQueryable<Notification> GetPermittedNotificationsQuery()
    {
        var currentUserId = _currentUserService.UserId;
        var isOperatorOrAdmin = _currentUserService.IsInRole(ApplicationRoles.Operator) ||
                                _currentUserService.IsInRole(ApplicationRoles.EmergencyAdmin) ||
                                _currentUserService.IsInRole(ApplicationRoles.SystemAdmin);
        var isResponder = _currentUserService.IsInRole(ApplicationRoles.Responder);
        var isCitizen = _currentUserService.IsInRole(ApplicationRoles.Citizen);
        var now = DateTime.UtcNow;

        return _context.Notifications
            .Include(n => n.Incident)
            .Include(n => n.OperationalSituation)
            .Where(n => n.ExpiresAtUtc == null || n.ExpiresAtUtc > now)
            .Where(n =>
                (n.UserId != null && n.UserId == currentUserId) ||
                (isOperatorOrAdmin && (n.TargetRole == ApplicationRoles.Operator ||
                                       n.TargetRole == ApplicationRoles.EmergencyAdmin ||
                                       n.TargetRole == ApplicationRoles.SystemAdmin)) ||
                (isResponder && n.TargetRole == ApplicationRoles.Responder) ||
                (isCitizen && n.TargetRole == ApplicationRoles.Citizen));
    }

    private bool IsNotificationVisibleToCurrentUser(Notification n)
    {
        var currentUserId = _currentUserService.UserId;
        if (n.UserId != null && n.UserId == currentUserId)
        {
            return true;
        }

        var isOperatorOrAdmin = _currentUserService.IsInRole(ApplicationRoles.Operator) ||
                                _currentUserService.IsInRole(ApplicationRoles.EmergencyAdmin) ||
                                _currentUserService.IsInRole(ApplicationRoles.SystemAdmin);

        if (isOperatorOrAdmin && (n.TargetRole == ApplicationRoles.Operator ||
                                 n.TargetRole == ApplicationRoles.EmergencyAdmin ||
                                 n.TargetRole == ApplicationRoles.SystemAdmin))
        {
            return true;
        }

        if (_currentUserService.IsInRole(ApplicationRoles.Responder) && n.TargetRole == ApplicationRoles.Responder)
        {
            return true;
        }

        if (_currentUserService.IsInRole(ApplicationRoles.Citizen) && n.TargetRole == ApplicationRoles.Citizen)
        {
            return true;
        }

        return false;
    }

    private static string ResolveActionUrl(Guid? incidentId, Guid? situationId)
    {
        if (incidentId.HasValue) return $"/Incidents/Details/{incidentId.Value}";
        if (situationId.HasValue) return $"/Situations/Details/{situationId.Value}";
        return "/Dashboard";
    }
}
