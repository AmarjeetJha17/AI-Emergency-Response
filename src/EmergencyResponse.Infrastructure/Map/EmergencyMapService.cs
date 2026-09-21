using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Map;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Map;

public sealed class EmergencyMapService : IEmergencyMapService
{
    private readonly ApplicationDbContext _db;
    private readonly MapOptions _options;

    public EmergencyMapService(
        ApplicationDbContext db,
        IOptions<MapOptions>? mapOptions = null)
    {
        _db = db;
        _options = mapOptions?.Value ?? new MapOptions();
    }

    public async Task<IReadOnlyList<EmergencyMapIncident>> GetIncidentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Incidents
            .AsNoTracking()
            .Include(x => x.IncidentType)
            .Include(x => x.Department)
            .Include(x => x.Locations)
            .Where(x =>
                x.Status != IncidentStatus.Closed &&
                x.Locations.Any())
            .Select(x => new EmergencyMapIncident
            {
                Id = x.Id,
                IncidentNumber = x.IncidentNumber,
                Title = x.Title,
                IncidentType = x.IncidentType != null ? x.IncidentType.Name : null,
                DepartmentName = x.Department != null ? x.Department.Name : null,
                Status = x.Status,
                Severity = x.Severity,
                Priority = x.Priority,
                Latitude = (double)(x.Locations.OrderByDescending(l => l.IsPrimary).First().Latitude),
                Longitude = (double)(x.Locations.OrderByDescending(l => l.IsPrimary).First().Longitude),
                Address = x.Locations.OrderByDescending(l => l.IsPrimary).First().Address,
                CreatedAtUtc = x.CreatedAtUtc,
                HasDuplicateCandidates = _db.IncidentDuplicateMatches.Any(
                    d => d.SourceIncidentId == x.Id &&
                         !d.Reviewed &&
                         d.DuplicateScore >= 0.75),
                HasVisualReview = _db.VisualAnalyses.Any(
                    v => v.IncidentId == x.Id &&
                         v.Status == VisualAnalysisStatus.RequiresReview &&
                         !v.HumanReviewed),
                RequiresAIReview = x.AIAnalyses.Any(
                    a => a.Status == AIAnalysisStatus.RequiresReview)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetIncidentTypesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.IncidentTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetDepartmentsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Departments
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MapResource>> GetResourcesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.Resources
            .AsNoTracking()
            .Include(x => x.Department)
            .Where(x =>
                x.ShowOnOperationalMap &&
                x.Latitude.HasValue &&
                x.Longitude.HasValue)
            .Select(x => new MapResource
            {
                Id = x.Id,
                Name = x.Name,
                ResourceType = x.Type,
                Status = x.Status,
                Latitude = (double)x.Latitude!.Value,
                Longitude = (double)x.Longitude!.Value,
                DepartmentName = x.Department != null ? x.Department.Name : null,
                LocationUpdatedAtUtc = x.LocationUpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public IReadOnlyList<MapOperationalZone> BuildOperationalZones(
        IReadOnlyList<EmergencyMapIncident> incidents)
    {
        var radii = _options.OperationalRadii;

        return incidents
            .Select(incident =>
            {
                var radius = incident.Severity switch
                {
                    SeverityLevel.Critical => radii.CriticalMeters,
                    SeverityLevel.High => radii.HighMeters,
                    SeverityLevel.Moderate => radii.ModerateMeters,
                    _ => radii.LowMeters
                };

                return radius <= 0
                    ? null
                    : new MapOperationalZone
                    {
                        IncidentId = incident.Id,
                        Latitude = incident.Latitude,
                        Longitude = incident.Longitude,
                        RadiusMeters = radius,
                        Severity = incident.Severity.ToString(),
                        Label = "Operational alert radius"
                    };
            })
            .Where(x => x is not null)
            .Cast<MapOperationalZone>()
            .ToList();
    }

    public async Task<IReadOnlyList<MapOperationalZone>> GetOperationalZonesAsync(
        CancellationToken cancellationToken = default)
    {
        var incidents = await GetIncidentsAsync(cancellationToken);
        return BuildOperationalZones(incidents);
    }
}
