using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;
using EmergencyResponse.Application.DTOs.Common;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using System.Text.Json;

namespace EmergencyResponse.Infrastructure.Services;

public sealed class IncidentService : IIncidentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IIncidentNumberGenerator _numberGenerator;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAIAnalysisQueue _aiQueue;

    public IncidentService(
        ApplicationDbContext dbContext,
        IIncidentNumberGenerator numberGenerator,
        ICurrentUserService currentUserService,
        IAIAnalysisQueue aiQueue)
    {
        _dbContext = dbContext;
        _numberGenerator = numberGenerator;
        _currentUserService = currentUserService;
        _aiQueue = aiQueue;
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


        await _aiQueue.EnqueueAsync(
            new IncidentAIJob(incident.Id),
            cancellationToken);

        return new IncidentResponse
        {
            Id = incident.Id,
            IncidentNumber = incident.IncidentNumber,
            Title = incident.Title,
            Description = incident.Description,
            IncidentType = incidentType.Name,
            Status = incident.Status.ToString(),
            DuplicateDetectionStatus = incident.DuplicateDetectionStatus,
            HasDuplicateCandidates = false,
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
                .Include(x => x.CanonicalIncident)
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

        if (incident is null)
        {
            return null;
        }

        var hasDuplicateCandidates =
            await _dbContext.IncidentDuplicateMatches
                .AnyAsync(
                    x =>
                        x.SourceIncidentId == incident.Id &&
                        !x.Reviewed &&
                        x.DuplicateScore >= 0.75,
                    cancellationToken);

        var location =
            incident.Locations
                .FirstOrDefault(x => x.IsPrimary)
            ?? incident.Locations.FirstOrDefault();

        var relatedIncidents =
            await _dbContext.IncidentRelations
                .AsNoTracking()
                .Where(
                    x => x.ParentIncidentId == incident.Id &&
                         x.RelationType == EmergencyResponse.Domain.Enums.IncidentRelationType.Duplicate)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(
                    x => new RelatedIncidentResponse
                    {
                        Id = x.RelatedIncidentId,
                        IncidentNumber = x.RelatedIncident.IncidentNumber,
                        RelationType = x.RelationType.ToString()
                    })
                .ToListAsync(cancellationToken);

        var mediaItems = await _dbContext.IncidentMedia
            .AsNoTracking()
            .Where(x => x.IncidentId == id)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var latestAnalyses = await _dbContext.VisualAnalyses
            .AsNoTracking()
            .Where(x => x.IncidentId == id)
            .GroupBy(x => x.IncidentMediaId)
            .Select(g => g.OrderByDescending(x => x.CreatedAtUtc).First())
            .ToListAsync(cancellationToken);

        var analysisByMediaId = latestAnalyses.ToDictionary(x => x.IncidentMediaId);

        var visualEvidence = mediaItems.Select(m =>
        {
            analysisByMediaId.TryGetValue(m.Id, out var analysis);

            VisualAnalysisResponse? analysisResponse = null;
            if (analysis is not null)
            {
                var findings = analysis.FindingsJson is not null
                    ? JsonSerializer.Deserialize<Application.AI.VisualAnalysis.VisualAnalysisResult>(
                        analysis.FindingsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    : null;

                analysisResponse = new VisualAnalysisResponse
                {
                    Id = analysis.Id,
                    IncidentMediaId = analysis.IncidentMediaId,
                    Status = analysis.Status,
                    SceneDescription = findings?.SceneDescription ?? string.Empty,
                    DetectedHazards = findings?.DetectedHazards ?? [],
                    VisibleEmergencyIndicators = findings?.VisibleEmergencyIndicators ?? [],
                    EstimatedPeopleAffected = findings?.EstimatedPeopleAffected,
                    EstimatedVehiclesAffected = findings?.EstimatedVehiclesAffected,
                    PeopleAppearTrapped = findings?.PeopleAppearTrapped ?? false,
                    RoadAppearsBlocked = findings?.RoadAppearsBlocked ?? false,
                    StructuralDamageVisible = findings?.StructuralDamageVisible ?? false,
                    FireVisible = findings?.FireVisible ?? false,
                    FloodingVisible = findings?.FloodingVisible ?? false,
                    SmokeVisible = findings?.SmokeVisible ?? false,
                    ImmediateDangerVisible = findings?.ImmediateDangerVisible ?? false,
                    RecommendedResponse = findings?.RecommendedResponse,
                    Confidence = findings?.Confidence ?? analysis.Confidence ?? 0,
                    HumanReviewed = analysis.HumanReviewed,
                    HumanDecision = analysis.HumanDecision,
                    FramesAnalyzed = analysis.FramesAnalyzed,
                    VideoDurationSeconds = analysis.VideoDurationSeconds,
                    FrameFindings = findings?.FrameFindings
                        .Select(f => new VisualFrameFindingResponse
                        {
                            FrameNumber = f.FrameNumber,
                            TimestampSeconds = f.TimestampSeconds,
                            SceneDescription = f.SceneDescription,
                            DetectedHazards = f.DetectedHazards,
                            EstimatedPeopleAffected = f.EstimatedPeopleAffected,
                            EstimatedVehiclesAffected = f.EstimatedVehiclesAffected,
                            PeopleAppearTrapped = f.PeopleAppearTrapped,
                            RoadAppearsBlocked = f.RoadAppearsBlocked,
                            StructuralDamageVisible = f.StructuralDamageVisible,
                            FireVisible = f.FireVisible,
                            FloodingVisible = f.FloodingVisible,
                            SmokeVisible = f.SmokeVisible,
                            ImmediateDangerVisible = f.ImmediateDangerVisible,
                            Confidence = f.Confidence
                        })
                        .ToList() ?? []
                };
            }

            return new IncidentMediaResponse
            {
                Id = m.Id,
                IncidentId = m.IncidentId,
                ContentType = m.ContentType,
                SizeBytes = m.SizeBytes,
                CreatedAtUtc = m.CreatedAtUtc,
                AIAnalysisRequested = m.AIAnalysisRequested,
                MediaType = m.MediaType,
                VisualAnalysisStatus = analysis?.Status,
                VisualAnalysisConfidence = analysis?.Confidence,
                MediaUrl = $"/api/incidents/{id}/media/{m.Id}",
                Analysis = analysisResponse
            };
        }).ToList();

        return new IncidentResponse
        {
            Id = incident.Id,
            IncidentNumber = incident.IncidentNumber,
            Title = incident.Title,
            Description = incident.Description,

            IncidentType = incident.IncidentType.Name,

            Status = incident.Status.ToString(),
            DuplicateDetectionStatus = incident.DuplicateDetectionStatus,
            HasDuplicateCandidates = hasDuplicateCandidates,
            CanonicalIncidentId = incident.CanonicalIncidentId,
            CanonicalIncidentNumber = incident.CanonicalIncident?.IncidentNumber,
            RelatedIncidents = relatedIncidents,
            Severity = incident.Severity.ToString(),
            Priority = incident.Priority.ToString(),

            Latitude = location?.Latitude ?? 0,
            Longitude = location?.Longitude ?? 0,

            Address = location?.Address,
            City = location?.City,

            PeopleAffected = incident.PeopleAffected,
            VehiclesAffected = incident.VehiclesAffected,
            Casualties = incident.Casualties,

            ReportedAtUtc = incident.ReportedAtUtc,
            VisualEvidence = visualEvidence
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