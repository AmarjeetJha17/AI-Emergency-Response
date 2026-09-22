using System.Text.Json;

using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.Resources;

public sealed class ResourceService : IResourceService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIncidentNotificationService _notificationService;
    private readonly ILogger<ResourceService> _logger;

    public ResourceService(
        ApplicationDbContext db,
        ICurrentUserService currentUserService,
        IIncidentNotificationService notificationService,
        ILogger<ResourceService> logger)
    {
        _db = db;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ResourceDetailsResponse>> GetResourcesAsync(
        CancellationToken cancellationToken = default)
    {
        var resources = await _db.Resources
            .AsNoTracking()
            .Include(r => r.Department)
            .Include(r => r.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.Incident)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return resources.Select(MapToDetailsResponse).ToList();
    }

    public async Task<ResourceDetailsResponse?> GetByIdAsync(
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var resource = await _db.Resources
            .AsNoTracking()
            .Include(r => r.Department)
            .Include(r => r.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.Incident)
            .SingleOrDefaultAsync(r => r.Id == resourceId, cancellationToken);

        return resource is null ? null : MapToDetailsResponse(resource);
    }

    public async Task<ResourceAssignmentResult> AssignAsync(
        Guid resourceId,
        Guid incidentId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var resource = await _db.Resources
            .SingleOrDefaultAsync(x => x.Id == resourceId, cancellationToken);

        if (resource is null)
        {
            throw new KeyNotFoundException("Resource not found.");
        }

        var incident = await _db.Incidents
            .SingleOrDefaultAsync(x => x.Id == incidentId, cancellationToken);

        if (incident is null)
        {
            throw new KeyNotFoundException("Incident not found.");
        }

        if (resource.Status != ResourceStatus.Available)
        {
            throw new InvalidOperationException("The resource is not available.");
        }

        var alreadyAssigned = await _db.ResourceAssignments
            .AnyAsync(x => x.ResourceId == resourceId && x.IsActive, cancellationToken);

        if (alreadyAssigned)
        {
            throw new InvalidOperationException("The resource already has an active assignment.");
        }

        // Department boundary validation
        if (incident.DepartmentId.HasValue && resource.DepartmentId != incident.DepartmentId.Value)
        {
            if (!_currentUserService.IsInRole("EmergencyAdmin") && !_currentUserService.IsInRole("SystemAdmin"))
            {
                throw new InvalidOperationException("The resource does not belong to the incident's department.");
            }
        }

        var now = DateTime.UtcNow;

        var assignment = new ResourceAssignment
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            IncidentId = incidentId,
            AssignedAtUtc = now,
            AssignedByUserId = _currentUserService.UserId,
            Remarks = notes,
            ReleaseNotes = null,
            IsActive = true,
            Status = ResourceAssignmentStatus.Active
        };

        _db.ResourceAssignments.Add(assignment);

        var previous = resource.Status;
        resource.Status = ResourceStatus.Assigned;
        resource.UpdatedAtUtc = now;

        _db.ResourceStatusHistories.Add(new ResourceStatusHistory
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            PreviousStatus = previous,
            NewStatus = ResourceStatus.Assigned,
            IncidentId = incidentId,
            ChangedByUserId = _currentUserService.UserId,
            Reason = notes,
            ChangedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "ResourceAssigned",
            EntityName = nameof(Resource),
            EntityId = resourceId.ToString(),
            NewValues = JsonSerializer.Serialize(new
            {
                incidentId,
                assignedBy = _currentUserService.UserId,
                notes
            }),
            OccurredAtUtc = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Resource {ResourceId} assigned to incident {IncidentId} by user {UserId}.",
            resourceId,
            incidentId,
            _currentUserService.UserId);

        await _notificationService.NotifyResourceUpdatedAsync(
            new ResourceUpdatedEvent
            {
                ResourceId = resourceId,
                IncidentId = incidentId,
                Reason = ResourceUpdateReasons.Assigned,
                UpdatedAtUtc = now
            },
            cancellationToken);

        return new ResourceAssignmentResult
        {
            AssignmentId = assignment.Id,
            ResourceId = resourceId,
            IncidentId = incidentId,
            AssignedAtUtc = now
        };
    }

    public async Task<ResourceStatusTransitionResult> ChangeStatusAsync(
        Guid resourceId,
        ResourceStatus newStatus,
        Guid? incidentId,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var resource = await _db.Resources
            .SingleOrDefaultAsync(x => x.Id == resourceId, cancellationToken);

        if (resource is null)
        {
            throw new KeyNotFoundException("Resource not found.");
        }

        var previous = resource.Status;

        if (previous == newStatus)
        {
            return new ResourceStatusTransitionResult
            {
                ResourceId = resourceId,
                PreviousStatus = previous,
                NewStatus = newStatus,
                ChangedAtUtc = DateTime.UtcNow
            };
        }

        if (!ResourceStatusTransitionRules.IsAllowed(previous, newStatus))
        {
            throw new InvalidOperationException(
                $"Transition from {previous} to {newStatus} is not allowed.");
        }

        var now = DateTime.UtcNow;

        var activeAssignment = await _db.ResourceAssignments
            .Where(x => x.ResourceId == resourceId && x.IsActive)
            .OrderByDescending(x => x.AssignedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeAssignment != null)
        {
            switch (newStatus)
            {
                case ResourceStatus.Dispatched:
                    activeAssignment.DispatchedAtUtc ??= now;
                    break;
                case ResourceStatus.EnRoute:
                    activeAssignment.EnRouteAtUtc ??= now;
                    break;
                case ResourceStatus.OnScene:
                    activeAssignment.OnSceneAtUtc ??= now;
                    break;
                case ResourceStatus.Returning:
                    activeAssignment.ReturningAtUtc ??= now;
                    break;
            }
        }

        resource.Status = newStatus;
        resource.UpdatedAtUtc = now;

        _db.ResourceStatusHistories.Add(new ResourceStatusHistory
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            PreviousStatus = previous,
            NewStatus = newStatus,
            IncidentId = incidentId ?? activeAssignment?.IncidentId,
            ChangedByUserId = _currentUserService.UserId,
            Reason = reason,
            ChangedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "ResourceStatusChanged",
            EntityName = nameof(Resource),
            EntityId = resourceId.ToString(),
            OldValues = JsonSerializer.Serialize(new { status = previous.ToString() }),
            NewValues = JsonSerializer.Serialize(new
            {
                status = newStatus.ToString(),
                incidentId = incidentId ?? activeAssignment?.IncidentId,
                reason
            }),
            OccurredAtUtc = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Resource {ResourceId} status changed from {Previous} to {NewStatus}.",
            resourceId,
            previous,
            newStatus);

        await _notificationService.NotifyResourceUpdatedAsync(
            new ResourceUpdatedEvent
            {
                ResourceId = resourceId,
                IncidentId = incidentId ?? activeAssignment?.IncidentId,
                Reason = ResourceUpdateReasons.StatusChanged,
                UpdatedAtUtc = now
            },
            cancellationToken);

        return new ResourceStatusTransitionResult
        {
            ResourceId = resourceId,
            PreviousStatus = previous,
            NewStatus = newStatus,
            ChangedAtUtc = now
        };
    }

    public async Task ReleaseAsync(
        Guid resourceId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _db.ResourceAssignments
            .Where(x => x.ResourceId == resourceId && x.IsActive)
            .OrderByDescending(x => x.AssignedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (assignment is null)
        {
            throw new InvalidOperationException("The resource has no active assignment.");
        }

        var resource = await _db.Resources
            .SingleOrDefaultAsync(x => x.Id == resourceId, cancellationToken);

        if (resource is null)
        {
            throw new KeyNotFoundException("Resource not found.");
        }

        var previous = resource.Status;

        if (previous != ResourceStatus.Available)
        {
            if (!ResourceStatusTransitionRules.IsAllowed(previous, ResourceStatus.Available))
            {
                throw new InvalidOperationException($"Resource cannot be released from {previous}.");
            }
        }

        var now = DateTime.UtcNow;

        assignment.IsActive = false;
        assignment.Status = ResourceAssignmentStatus.Completed;
        assignment.ReleasedAtUtc = now;
        assignment.ReleaseNotes = notes;

        resource.Status = ResourceStatus.Available;
        resource.UpdatedAtUtc = now;

        _db.ResourceStatusHistories.Add(new ResourceStatusHistory
        {
            Id = Guid.NewGuid(),
            ResourceId = resourceId,
            PreviousStatus = previous,
            NewStatus = ResourceStatus.Available,
            IncidentId = assignment.IncidentId,
            ChangedByUserId = _currentUserService.UserId,
            Reason = notes,
            ChangedAtUtc = now
        });

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "ResourceReleased",
            EntityName = nameof(Resource),
            EntityId = resourceId.ToString(),
            NewValues = JsonSerializer.Serialize(new
            {
                assignmentId = assignment.Id,
                incidentId = assignment.IncidentId,
                notes
            }),
            OccurredAtUtc = now
        });

        await _db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Resource {ResourceId} released from incident {IncidentId}.",
            resourceId,
            assignment.IncidentId);

        await _notificationService.NotifyResourceUpdatedAsync(
            new ResourceUpdatedEvent
            {
                ResourceId = resourceId,
                IncidentId = assignment.IncidentId,
                Reason = ResourceUpdateReasons.Released,
                UpdatedAtUtc = now
            },
            cancellationToken);
    }

    private static ResourceDetailsResponse MapToDetailsResponse(Resource r)
    {
        var active = r.Assignments.FirstOrDefault(a => a.IsActive);

        return new ResourceDetailsResponse
        {
            Id = r.Id,
            Name = r.Name,
            ResourceType = r.Type,
            Status = r.Status,
            DepartmentName = r.Department?.Name,
            Latitude = r.Latitude != null ? (double?)r.Latitude.Value : null,
            Longitude = r.Longitude != null ? (double?)r.Longitude.Value : null,
            LocationUpdatedAtUtc = r.LocationUpdatedAtUtc,
            ActiveIncidentId = active?.IncidentId,
            ActiveIncidentNumber = active?.Incident?.IncidentNumber
        };
    }
}
