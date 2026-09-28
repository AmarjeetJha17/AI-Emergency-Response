# Operational Notification & Alerting System Architecture

## 1. Overview & Architectural Principles

The **Operational Notification & Alerting System** connects the intelligence, escalation, resource management, and situational awareness layers to human operators, dispatchers, and emergency administrators.

The system adheres to an essential architectural principle:
> **SQL Server is the authoritative single source of truth for all notifications; SignalR is strictly the delivery mechanism.**

```
Incident / AI / Visual / Escalation / Situation / Resource Events
                               │
                               ▼
                   INotificationRuleEngine
                               │
                               ▼
                      NotificationRequest
                               │
                               ▼
                      NotificationService
                               │
                ┌──────────────┴──────────────┐
                ▼                             ▼
       Authoritative Storage           Real-Time Push
            (SQL Server)                  (SignalR)
                │                             │
                ▼                             ▼
       Operator Notification          Navbar Bell & Badge
          Center & Feed               Floating Toast Alerts
       (/Notifications)              Live Dashboard KPIs
```

---

## 2. Anti-Flooding Deduplication

Background evaluation workers (such as escalation checks and clustering engines) run periodically every few seconds or minutes. Without deduplication, continuous background evaluations would generate flooding alert storms for ongoing critical incidents.

To prevent alert fatigue while ensuring immediate notification of new risks:
1. Every rule-generated notification generates a deterministic **`DeduplicationKey`**:
   - `EscalationOpened:{escalationId}`
   - `SituationCreated:{situationId}`
   - `CriticalSituation:{situationId}`
   - `ResourceShortage:{situationOrIncidentId}`
   - `AIReview:{incidentId}`
   - `DuplicateReview:{incidentId}`
   - `VisualReview:{incidentId}`
   - `PeopleTrapped:{incidentId}`
   - `ImmediateDanger:{incidentId}`
2. **Suppression Logic**:
   - Before inserting a new notification, the database is queried:
     ```csharp
     var exists = await _context.Notifications.AnyAsync(n =>
         n.DeduplicationKey == request.DeduplicationKey &&
         !n.IsRead &&
         n.Status == NotificationStatus.Unread &&
         (n.ExpiresAtUtc == null || n.ExpiresAtUtc > now));
     ```
   - If an unread, non-expired notification with the exact deduplication key already exists, creation is suppressed and logged at informational level.
   - Once an operator marks the notification as read, or after expiration, subsequent state changes can generate a fresh notification if necessary.

---

## 3. Targeted Visibility & Zero-Trust Scoping

Notifications are targeted using three orthogonal vectors:
1. `UserId`: Direct individual targeting (e.g. assigned responder, specific reporting citizen).
2. `TargetRole`: Role-scoped targeting (`Operator`, `EmergencyAdmin`, `SystemAdmin`, `Responder`, `Citizen`).
3. `TargetDepartmentId`: Department-specific targeting (e.g. Fire, Police, EMS).

### Zero-Trust Enforcement
Client requests never specify user identity in query strings or payloads. The [`NotificationService`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Infrastructure/Notifications/NotificationService.cs) automatically derives user identity and claims from [`ICurrentUserService`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Application/Interfaces/ICurrentUserService.cs):

```csharp
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
        .Where(n => n.ExpiresAtUtc == null || n.ExpiresAtUtc > now)
        .Where(n =>
            (n.UserId != null && n.UserId == currentUserId) ||
            (isOperatorOrAdmin && (n.TargetRole == ApplicationRoles.Operator ||
                                   n.TargetRole == ApplicationRoles.EmergencyAdmin ||
                                   n.TargetRole == ApplicationRoles.SystemAdmin)) ||
            (isResponder && n.TargetRole == ApplicationRoles.Responder) ||
            (isCitizen && n.TargetRole == ApplicationRoles.Citizen));
}
```

Attempts by unauthorized callers to read or mark another user's notifications as read throw `UnauthorizedAccessException`, resulting in an HTTP `403 Forbidden` response.

---

## 4. SignalR Targeted Group Dispatch

When a notification is persisted to the database, [`IncidentNotificationService`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Api/Services/IncidentNotificationService.cs) dispatches the real-time event to authorized SignalR groups:

```csharp
if (!string.IsNullOrWhiteSpace(notification.UserId))
{
    await _hubContext.Clients
        .Group($"user:{notification.UserId}")
        .SendAsync("NotificationCreated", eventPayload, cancellationToken);
}
else if (!string.IsNullOrWhiteSpace(notification.TargetRole))
{
    if (notification.TargetRole is ApplicationRoles.Operator or ApplicationRoles.EmergencyAdmin or ApplicationRoles.SystemAdmin)
    {
        await _hubContext.Clients
            .Group("dashboard-operators")
            .SendAsync("NotificationCreated", eventPayload, cancellationToken);
    }
    else
    {
        await _hubContext.Clients
            .Group($"role:{notification.TargetRole}")
            .SendAsync("NotificationCreated", eventPayload, cancellationToken);
    }
}
```

During SignalR connection handshake in [`IncidentHub`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Api/Hubs/IncidentHub.cs), client tokens are authenticated, and callers are placed into corresponding SignalR groups based on verified JWT claims.

---

## 5. UI Integration & User Experience

The notification frontend comprises four synchronized presentation layers:

1. **Navbar Notification Bell & Live Badge** ([`_Layout.cshtml`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Web/Views/Shared/_Layout.cshtml)):
   - Always visible in global layout.
   - Dynamic counter badge for unread notifications with live updates via SignalR.
   - Dropdown drawer previewing recent alerts with quick "Mark read" and "View" actions.

2. **Floating Bootstrap Toast Alerts** ([`notifications.js`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Web/wwwroot/js/notifications.js)):
   - Instant non-intrusive popups in the bottom-right corner when new critical or urgent events arrive.
   - Color-coded borders (`border-danger` for Critical, `border-warning` for Urgent, `border-primary` for Important).
   - Deep links directly navigating to `[View Incident]` or `[View Situation]`.

3. **Dedicated Notification Center** ([`Index.cshtml`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Web/Views/Notifications/Index.cshtml)):
   - Accessible via `/Notifications`.
   - Filter pills for All, Unread, Critical, Urgent, Important, and Read.
   - Direct batch action: "Mark All Read".
   - Rich operational cards highlighting escalation indicators, timestamp, and entity references.

4. **Executive Operations Dashboard KPI Integration** ([`Dashboard/Index.cshtml`](file:///D:/Amarjeet/Projects/AI-Emergency-Response/src/EmergencyResponse.Web/Views/Dashboard/Index.cshtml)):
   - Real-time indicator cards for Unread Operator Alerts, Critical Alert Notifications, and Urgent Alert Notifications.
   - Instantly updated via `incident-realtime.js` whenever `NotificationCreated` or `DashboardUpdated` events occur.

---

## 6. Verification & Quality Metrics

The implementation is verified with automated tests covering all critical paths:
- **Unit Tests** (`tests/EmergencyResponse.UnitTests/Notifications/`):
  - `NotificationRuleEngineTests`: Evaluates escalation level mapping, people trapped rules, immediate danger alerts, situation cluster triggers, resource shortage detection, and review queue alerts.
  - `NotificationServiceTests`: Verifies DB persistence, SignalR event dispatch, deduplication suppression, expired key re-creation, zero-trust role isolation, unread summary computation, and mark-as-read/mark-all-as-read semantics.
- **Integration Tests** (`tests/EmergencyResponse.IntegrationTests/Notifications/`):
  - `NotificationApiTests`: Validates HTTP endpoints (`GET /api/notifications`, `GET /api/notifications/summary`, `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`), 401 Unauthorized handling, 403 Forbidden role isolation, 404 NotFound handling, and complete citizen vs operator access boundaries.
