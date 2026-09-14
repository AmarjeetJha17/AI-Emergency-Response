using EmergencyResponse.Application.DTOs.Common;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Services;

public sealed class IncidentService : IIncidentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IIncidentNumberGenerator _numberGenerator;
    private readonly ICurrentUserService _currentUserService;

    public IncidentService(
        ApplicationDbContext dbContext,
        IIncidentNumberGenerator numberGenerator,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _numberGenerator = numberGenerator;
        _currentUserService = currentUserService;
    }

    public async Task<IncidentResponse> CreateAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        var incidentType =
            await _dbContext.IncidentTypes
                .FirstOrDefaultAsync(
                    x => x.Id == request.IncidentTypeId
                         && x.IsActive,
                    cancellationToken);

        if (incidentType is null)
        {
            throw new KeyNotFoundException(
                "The selected incident type does not exist.");
        }

        var incidentNumber =
            await _numberGenerator.GenerateAsync(
                cancellationToken);

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            ReporterUserId = _currentUserService.UserId,
            IncidentNumber = incidentNumber,
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),

            IncidentTypeId = incidentType.Id,

            Status = IncidentStatus.Submitted,

            Severity = SeverityLevel.Low,

            Priority = PriorityLevel.Normal,

            PeopleAffected = request.PeopleAffected,
            VehiclesAffected = request.VehiclesAffected,
            Casualties = request.Casualties,

            ReportedAtUtc = DateTime.UtcNow
        };

        incident.Locations.Add(
            new IncidentLocation
            {
                Id = Guid.NewGuid(),
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Address = request.Address?.Trim(),
                City = request.City?.Trim(),
                District = request.District?.Trim(),
                State = request.State?.Trim(),
                IsPrimary = true
            });

        incident.StatusHistory.Add(
            new IncidentStatusHistory
            {
                Id = Guid.NewGuid(),
                OldStatus = IncidentStatus.Submitted,
                NewStatus = IncidentStatus.Submitted,
                ChangedAtUtc = DateTime.UtcNow,
                Remarks = "Incident created."
            });

        _dbContext.Incidents.Add(incident);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        return new IncidentResponse
        {
            Id = incident.Id,
            IncidentNumber = incident.IncidentNumber,
            Title = incident.Title,
            Description = incident.Description,
            IncidentType = incidentType.Name,
            Status = incident.Status.ToString(),
            Severity = incident.Severity.ToString(),
            Priority = incident.Priority.ToString(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Address = request.Address,
            City = request.City,
            PeopleAffected = incident.PeopleAffected,
            VehiclesAffected = incident.VehiclesAffected,
            Casualties = incident.Casualties,
            ReportedAtUtc = incident.ReportedAtUtc
        };
    }

    public async Task<IncidentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var incident =
            await _dbContext.Incidents
                .AsNoTracking()
                .Include(x => x.IncidentType)
                .Include(x => x.Locations)
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

        if (incident is null)
        {
            return null;
        }

        var location =
            incident.Locations
                .FirstOrDefault(x => x.IsPrimary)
            ?? incident.Locations.FirstOrDefault();

        return new IncidentResponse
        {
            Id = incident.Id,
            IncidentNumber = incident.IncidentNumber,
            Title = incident.Title,
            Description = incident.Description,

            IncidentType = incident.IncidentType.Name,

            Status = incident.Status.ToString(),
            Severity = incident.Severity.ToString(),
            Priority = incident.Priority.ToString(),

            Latitude = location?.Latitude ?? 0,
            Longitude = location?.Longitude ?? 0,

            Address = location?.Address,
            City = location?.City,

            PeopleAffected = incident.PeopleAffected,
            VehiclesAffected = incident.VehiclesAffected,
            Casualties = incident.Casualties,

            ReportedAtUtc = incident.ReportedAtUtc
        };
    }

    public Task<PagedResult<IncidentListItem>> GetPagedAsync(
    int page,
    int pageSize,
    string? search,
    Guid? incidentTypeId,
    string? status,
    string? severity,
    CancellationToken cancellationToken = default)
    {
        return GetPagedInternalAsync(
            page,
            pageSize,
            search,
            incidentTypeId,
            status,
            severity,
            reporterUserId: null,
            cancellationToken);
    }

    public Task<PagedResult<IncidentListItem>> GetPagedForCurrentUserAsync(
        int page,
        int pageSize,
        string? search,
        Guid? incidentTypeId,
        string? status,
        string? severity,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;

        if (!_currentUserService.IsAuthenticated
            || string.IsNullOrWhiteSpace(userId))
        {
            throw new UnauthorizedAccessException(
                "The current user could not be identified.");
        }

        return GetPagedInternalAsync(
            page,
            pageSize,
            search,
            incidentTypeId,
            status,
            severity,
            userId,
            cancellationToken);
    }

    private async Task<PagedResult<IncidentListItem>> GetPagedInternalAsync(
        int page,
        int pageSize,
        string? search,
        Guid? incidentTypeId,
        string? status,
        string? severity,
        string? reporterUserId,
        CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Incidents
            .AsNoTracking()
            .Include(x => x.IncidentType)
            .Include(x => x.Locations)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(reporterUserId))
        {
            query = query.Where(
                x => x.ReporterUserId == reporterUserId);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(x =>
                x.IncidentNumber.Contains(search) ||
                x.Title.Contains(search) ||
                x.Description.Contains(search));
        }

        if (incidentTypeId.HasValue)
        {
            query = query.Where(
                x => x.IncidentTypeId == incidentTypeId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<IncidentStatus>(
                status,
                true,
                out var parsedStatus))
        {
            query = query.Where(
                x => x.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(severity)
            && Enum.TryParse<SeverityLevel>(
                severity,
                true,
                out var parsedSeverity))
        {
            query = query.Where(
                x => x.Severity == parsedSeverity);
        }

        var totalCount =
            await query.CountAsync(cancellationToken);

        var items =
            await query
                .OrderByDescending(x => x.ReportedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new IncidentListItem
                {
                    Id = x.Id,
                    IncidentNumber = x.IncidentNumber,
                    Title = x.Title,
                    IncidentType = x.IncidentType.Name,
                    Status = x.Status.ToString(),
                    Severity = x.Severity.ToString(),
                    Priority = x.Priority.ToString(),
                    City = x.Locations
                        .Where(l => l.IsPrimary)
                        .Select(l => l.City)
                        .FirstOrDefault(),
                    ReportedAtUtc = x.ReportedAtUtc
                })
                .ToListAsync(cancellationToken);

        return new PagedResult<IncidentListItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }
}