using System.Text.Json;

using EmergencyResponse.Application.Common.Geo;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Resources;

public sealed class ResourceService : IResourceService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIncidentNotificationService _notificationService;
    private readonly ILogger<ResourceService> _logger;
    private readonly ResourceRecommendationOptions _options;

    public ResourceService(
        ApplicationDbContext db,
        ICurrentUserService currentUserService,
        IIncidentNotificationService notificationService,
        ILogger<ResourceService> logger,
        IOptions<ResourceRecommendationOptions>? options = null)
    {
        _db = db;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _logger = logger;
        _options = options?.Value ?? new ResourceRecommendationOptions();
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

        return resources.Select(r => MapToDetailsResponse(r)).ToList();
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

        if (resource is null)
        {
            return null;
        }

        var history = await _db.ResourceStatusHistories
            .AsNoTracking()
            .Include(h => h.Incident)
            .Where(h => h.ResourceId == resourceId)
            .OrderByDescending(h => h.ChangedAtUtc)
            .Select(h => new ResourceStatusHistoryItem
            {
                PreviousStatus = h.PreviousStatus,
                NewStatus = h.NewStatus,
                Reason = h.Reason,
                ChangedByUserId = h.ChangedByUserId,
                ChangedAtUtc = h.ChangedAtUtc,
                IncidentNumber = h.Incident != null ? h.Incident.IncidentNumber : null
            })
            .ToListAsync(cancellationToken);

        return MapToDetailsResponse(resource, history);
    }

    public async Task<IReadOnlyList<ResourceCommandCenterItem>> GetCommandCenterAsync(
        CancellationToken cancellationToken = default)
    {
        var resources = await _db.Resources
            .AsNoTracking()
            .Include(r => r.Department)
            .Include(r => r.Assignments.Where(a => a.IsActive))
                .ThenInclude(a => a.Incident)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return resources.Select(r =>
        {
            var active = r.Assignments.FirstOrDefault(a => a.IsActive);
            return new ResourceCommandCenterItem
            {
                Id = r.Id,
                Name = r.Name,
                ResourceType = r.Type,
                Status = r.Status,
                DepartmentName = r.Department?.Name,
                ActiveIncidentId = active?.IncidentId,
                ActiveIncidentNumber = active?.Incident?.IncidentNumber,
                ActiveIncidentTitle = active?.Incident?.Title,
                Latitude = r.Latitude != null ? (double?)r.Latitude.Value : null,
                Longitude = r.Longitude != null ? (double?)r.Longitude.Value : null,
                LocationUpdatedAtUtc = r.LocationUpdatedAtUtc,
                AssignedAtUtc = active?.AssignedAtUtc,
                EnRouteAtUtc = active?.EnRouteAtUtc,
                OnSceneAtUtc = active?.OnSceneAtUtc
            };
        }).ToList();
    }

    public async Task<ResourceSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var statusCounts = await _db.Resources
            .AsNoTracking()
            .GroupBy(r => r.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var dict = statusCounts.ToDictionary(x => x.Status, x => x.Count);

        return new ResourceSummary
        {
            Available = dict.GetValueOrDefault(ResourceStatus.Available, 0),
            Assigned = dict.GetValueOrDefault(ResourceStatus.Assigned, 0),
            Dispatched = dict.GetValueOrDefault(ResourceStatus.Dispatched, 0),
            EnRoute = dict.GetValueOrDefault(ResourceStatus.EnRoute, 0),
            OnScene = dict.GetValueOrDefault(ResourceStatus.OnScene, 0),
            Returning = dict.GetValueOrDefault(ResourceStatus.Returning, 0),
            Unavailable = dict.GetValueOrDefault(ResourceStatus.Unavailable, 0),
            Maintenance = dict.GetValueOrDefault(ResourceStatus.Maintenance, 0)
        };
    }

    public async Task<IReadOnlyList<AssignableIncidentItem>> GetAssignableIncidentsAsync(
        Guid? resourceId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Incidents
            .AsNoTracking()
            .Include(i => i.Department)
            .Where(i => i.Status != IncidentStatus.Closed);

        if (resourceId.HasValue)
        {
            var resource = await _db.Resources
                .AsNoTracking()
                .SingleOrDefaultAsync(r => r.Id == resourceId.Value, cancellationToken);

            if (resource is not null &&
                !_currentUserService.IsInRole("EmergencyAdmin") &&
                !_currentUserService.IsInRole("SystemAdmin"))
            {
                query = query.Where(i => !i.DepartmentId.HasValue || i.DepartmentId == resource.DepartmentId);
            }
        }

        var incidents = await query
            .OrderByDescending(i => i.Priority)
            .ThenByDescending(i => i.CreatedAtUtc)
            .Select(i => new AssignableIncidentItem
            {
                Id = i.Id,
                IncidentNumber = i.IncidentNumber,
                Title = i.Title,
                Severity = i.Severity,
                Priority = i.Priority,
                DepartmentName = i.Department != null ? i.Department.Name : null
            })
            .ToListAsync(cancellationToken);

        return incidents;
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

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict detected while assigning resource {ResourceId}.", resourceId);
            throw new InvalidOperationException("The resource was modified by another operator. Refresh the recommendations and try again.", ex);
        }
        catch (DbUpdateException ex) when (IsActiveAssignmentConflict(ex))
        {
            _logger.LogWarning(ex, "Active assignment conflict detected for resource {ResourceId}.", resourceId);
            throw new InvalidOperationException("The resource already has an active assignment.", ex);
        }

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

    private static ResourceDetailsResponse MapToDetailsResponse(
        Resource r,
        IReadOnlyList<ResourceStatusHistoryItem>? history = null)
    {
        var active = r.Assignments.FirstOrDefault(a => a.IsActive);

        return new ResourceDetailsResponse
        {
            Id = r.Id,
            Name = r.Name,
            ResourceType = r.Type,
            Status = r.Status,
            DepartmentName = r.Department?.Name,
            Capacity = r.Capacity,
            ShowOnOperationalMap = r.ShowOnOperationalMap,
            Latitude = r.Latitude != null ? (double?)r.Latitude.Value : null,
            Longitude = r.Longitude != null ? (double?)r.Longitude.Value : null,
            LocationUpdatedAtUtc = r.LocationUpdatedAtUtc,
            ActiveIncidentId = active?.IncidentId,
            ActiveIncidentNumber = active?.Incident?.IncidentNumber,
            ActiveIncidentTitle = active?.Incident?.Title,
            AssignedAtUtc = active?.AssignedAtUtc,
            DispatchedAtUtc = active?.DispatchedAtUtc,
            EnRouteAtUtc = active?.EnRouteAtUtc,
            OnSceneAtUtc = active?.OnSceneAtUtc,
            ReturningAtUtc = active?.ReturningAtUtc,
            AssignedByUserId = active?.AssignedByUserId,
            AssignmentRemarks = active?.Remarks,
            StatusHistory = history ?? []
        };
    }

    public async Task<ResourceRecommendationResponse> GetRecommendationsAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var incident = await _db.Incidents
            .AsNoTracking()
            .Include(i => i.IncidentType)
            .Include(i => i.Department)
            .Include(i => i.Locations)
            .SingleOrDefaultAsync(i => i.Id == incidentId, cancellationToken);

        if (incident is null)
        {
            throw new KeyNotFoundException($"Incident {incidentId} not found.");
        }

        var primaryLocation = incident.Locations
            .OrderByDescending(l => l.IsPrimary)
            .ThenBy(l => l.CreatedAtUtc)
            .FirstOrDefault();

        var requirements = await _db.IncidentTypeResourceRequirements
            .AsNoTracking()
            .Where(r => r.IncidentTypeId == incident.IncidentTypeId)
            .OrderBy(r => r.PriorityOrder)
            .ToListAsync(cancellationToken);

        var requirementDtos = requirements.Select(r => new ResourceRequirementDto
        {
            ResourceType = r.ResourceType,
            MinimumCount = r.MinimumCount,
            PreferredCount = r.PreferredCount,
            IsMandatory = r.IsMandatory,
            PriorityOrder = r.PriorityOrder
        }).ToList();

        if (requirements.Count == 0)
        {
            return new ResourceRecommendationResponse
            {
                IncidentId = incident.Id,
                IncidentNumber = incident.IncidentNumber,
                IncidentTitle = incident.Title,
                IncidentType = incident.IncidentType?.Name,
                Severity = incident.Severity.ToString(),
                Priority = incident.Priority.ToString(),
                DepartmentName = incident.Department?.Name,
                Requirements = [],
                Recommendations = []
            };
        }

        var requiredResourceTypes = requirements.Select(r => r.ResourceType).ToHashSet();
        var maxDistanceKm = _options.MaximumDistanceKm > 0 ? _options.MaximumDistanceKm : 50.0;

        var resources = await _db.Resources
            .AsNoTracking()
            .Include(r => r.Department)
            .Include(r => r.Assignments.Where(a => a.IsActive))
            .Where(r => requiredResourceTypes.Contains(r.Type) ||
                        (incident.DepartmentId.HasValue && r.DepartmentId == incident.DepartmentId.Value))
            .ToListAsync(cancellationToken);

        var recommendations = new List<ResourceRecommendation>();

        foreach (var resource in resources)
        {
            var hasActiveAssignment = resource.Assignments.Any(a => a.IsActive);

            double? distanceKm = null;
            if (primaryLocation is not null &&
                resource.Latitude.HasValue &&
                resource.Longitude.HasValue)
            {
                distanceKm = Math.Round(
                    GeoDistance.CalculateKilometers(
                        (double)primaryLocation.Latitude,
                        (double)primaryLocation.Longitude,
                        (double)resource.Latitude.Value,
                        (double)resource.Longitude.Value),
                    1);
            }

            var distanceScore = ResourceDistanceScorer.Calculate(distanceKm, maxDistanceKm);
            var availabilityScore = (resource.Status == ResourceStatus.Available && !hasActiveAssignment) ? 1.0 : 0.0;
            var departmentScore = (!incident.DepartmentId.HasValue || resource.DepartmentId == incident.DepartmentId.Value) ? 1.0 : 0.0;
            var suitabilityScore = requiredResourceTypes.Contains(resource.Type) ? 1.0 : 0.0;

            var compositeScore = ResourceRecommendationScorer.Calculate(
                distanceScore,
                availabilityScore,
                departmentScore,
                suitabilityScore);

            var reasons = new List<string>();
            if (availabilityScore > 0.0)
            {
                reasons.Add("Unit is Available with no active assignments (+20%)");
            }
            if (departmentScore > 0.0)
            {
                var deptName = incident.Department?.Name ?? resource.Department?.Name ?? "Incident Department";
                reasons.Add($"Assigned to incident department '{deptName}' (+20%)");
            }
            if (suitabilityScore > 0.0)
            {
                var req = requirements.FirstOrDefault(r => r.ResourceType == resource.Type);
                var priorityText = req?.IsMandatory == true ? "Mandatory" : "Preferred";
                reasons.Add($"Matches required resource type '{resource.Type}' ({priorityText}, Priority {req?.PriorityOrder ?? 1}) (+10%)");
            }
            if (distanceKm.HasValue)
            {
                var distPts = Math.Round(distanceScore * 50.0, 1);
                reasons.Add($"Located {distanceKm.Value:F1} km from incident scene (+{distPts}% distance score)");
            }
            else
            {
                reasons.Add("Location coordinates unavailable for precise distance scoring (0%)");
            }

            var exclusionReasons = new List<string>();
            if (resource.Status != ResourceStatus.Available)
            {
                exclusionReasons.Add($"Current status is '{resource.Status}', not Available");
            }
            if (hasActiveAssignment)
            {
                exclusionReasons.Add("Resource currently has an active incident assignment");
            }
            if (incident.DepartmentId.HasValue && resource.DepartmentId != incident.DepartmentId.Value)
            {
                exclusionReasons.Add($"Belongs to '{resource.Department?.Name ?? "Department"}', not incident department '{incident.Department?.Name ?? "Incident Department"}'");
            }
            if (!requiredResourceTypes.Contains(resource.Type))
            {
                exclusionReasons.Add($"Resource type '{resource.Type}' is not among required types for this incident");
            }
            if (distanceKm.HasValue && distanceKm.Value > maxDistanceKm)
            {
                exclusionReasons.Add($"Distance ({distanceKm.Value:F1} km) exceeds maximum operational radius ({maxDistanceKm} km)");
            }

            var isEligible = exclusionReasons.Count == 0;

            recommendations.Add(new ResourceRecommendation
            {
                ResourceId = resource.Id,
                ResourceName = resource.Name,
                ResourceType = resource.Type,
                Status = resource.Status,
                DepartmentName = resource.Department?.Name,
                DistanceKm = distanceKm,
                RecommendationScore = compositeScore,
                Eligible = isEligible,
                Reasons = reasons,
                ExclusionReasons = exclusionReasons,
                IncidentId = incident.Id,
                IncidentNumber = incident.IncidentNumber
            });
        }

        var ranked = recommendations
            .OrderByDescending(r => r.Eligible)
            .ThenByDescending(r => r.RecommendationScore)
            .ThenBy(r => r.DistanceKm ?? double.MaxValue)
            .ThenBy(r => r.ResourceName)
            .Take(25)
            .ToList();

        return new ResourceRecommendationResponse
        {
            IncidentId = incident.Id,
            IncidentNumber = incident.IncidentNumber,
            IncidentTitle = incident.Title,
            IncidentType = incident.IncidentType?.Name,
            Severity = incident.Severity.ToString(),
            Priority = incident.Priority.ToString(),
            DepartmentName = incident.Department?.Name,
            Requirements = requirementDtos,
            Recommendations = ranked
        };
    }

    private static bool IsActiveAssignmentConflict(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? ex.Message;
        return message.Contains("IX_ResourceAssignments_ResourceId", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("ResourceAssignments.ResourceId", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("UNIQUE constraint failed: ResourceAssignments.ResourceId", StringComparison.OrdinalIgnoreCase);
    }
}
