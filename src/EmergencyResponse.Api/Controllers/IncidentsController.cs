using System.Text.Json;

using EmergencyResponse.Api.Common;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.DuplicateDetection;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;
using EmergencyResponse.Application.AI.VisualAnalysis;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Application.Predictive;
using EmergencyResponse.Application.Predictive.Interfaces;
using EmergencyResponse.Application.Predictive.Jobs;
using EmergencyResponse.Application.Intelligence;

using EmergencyResponse.Application.DTOs.Common;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Application.Security.Authorization;
using EmergencyResponse.Application.Security.SecurityAudit;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Api.Controllers;

[ApiController]
[Route("api/incidents")]
public class IncidentsController : ControllerBase
{
    private readonly IIncidentService _incidentService;
    private readonly IDuplicateIncidentService _duplicateIncidentService;
    private readonly IIncidentConsolidationService _incidentConsolidationService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly IIncidentNotificationService _notificationService;
    private readonly IIncidentMediaService _incidentMediaService;
    private readonly IVisualAnalysisQueue _visualAnalysisQueue;
    private readonly IIncidentMediaStorage _mediaStorage;
    private readonly IAIAnalysisQueue? _aiAnalysisQueue;
    private readonly IEscalationRiskPredictionService _predictionService;
    private readonly IPredictiveAnalysisQueue? _predictiveAnalysisQueue;
    private readonly bool _aiEnabled;

    public IncidentsController(
        IIncidentService incidentService,
        IDuplicateIncidentService duplicateIncidentService,
        IIncidentConsolidationService incidentConsolidationService,
        ApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IIncidentNotificationService notificationService,
        IIncidentMediaService incidentMediaService,
        IVisualAnalysisQueue visualAnalysisQueue,
        IIncidentMediaStorage mediaStorage,
        IAIAnalysisQueue? aiAnalysisQueue = null,
        IEscalationRiskPredictionService? predictionService = null,
        IPredictiveAnalysisQueue? predictiveAnalysisQueue = null,
        IOptions<AIOptions>? aiOptions = null)
    {
        _incidentService = incidentService;
        _duplicateIncidentService = duplicateIncidentService;
        _incidentConsolidationService = incidentConsolidationService;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
        _incidentMediaService = incidentMediaService;
        _visualAnalysisQueue = visualAnalysisQueue;
        _mediaStorage = mediaStorage;
        _aiAnalysisQueue = aiAnalysisQueue;
        _predictionService = predictionService
            ?? throw new ArgumentNullException(nameof(predictionService));
        _predictiveAnalysisQueue = predictiveAnalysisQueue;
        _aiEnabled = aiOptions?.Value.Enabled ?? true;
    }

    [Authorize]
    [HttpPost]
    [Authorize(Policy = Permissions.IncidentsCreate)]
    [EnableRateLimiting("incident-create")]
    public async Task<ActionResult<IncidentResponse>> Create(
        CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _incidentService.CreateAsync(
                request,
                cancellationToken);

        await _notificationService.NotifyDashboardUpdatedAsync(
            new DashboardUpdatedEvent
            {
                Reason = DashboardUpdateReasons.IncidentCreated,
                IncidentId = result.Id,
                UpdatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            result);
    }

    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<IncidentResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result =
            await _incidentService.GetByIdAsync(
                id,
                cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [Authorize(Roles = "Responder,Operator,EmergencyAdmin,SystemAdmin")]
    [HttpGet("{id:guid}/environment")]
    public async Task<ActionResult<object>> GetEnvironment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var context = await _dbContext.IncidentEnvironmentalContexts
            .AsNoTracking()
            .Where(x => x.IncidentId == id)
            .OrderByDescending(x => x.ObservedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return context is null
            ? NotFound()
            : Ok(new
            {
                context.IncidentId,
                context.ObservedAtUtc,
                context.Provider,
                context.ProviderVersion,
                context.Latitude,
                context.Longitude,
                context.TemperatureC,
                context.ApparentTemperatureC,
                context.RelativeHumidity,
                context.WindSpeedKph,
                context.WindDirectionDegrees,
                context.PrecipitationMm,
                context.VisibilityKm,
                context.PressureHpa,
                context.CloudCoverPercent,
                context.WeatherPhrase,
                context.HasSevereWeatherAlert,
                context.SevereWeatherAlertTypesJson,
                context.AirQualityCategory,
                context.AirQualityIndex,
                context.ContextHash
            });
    }

    [Authorize(Roles = "Responder,Operator,EmergencyAdmin,SystemAdmin")]
    [HttpGet("{id:guid}/context")]
    public async Task<ActionResult<IncidentContextSnapshot>> GetContext(
        Guid id,
        CancellationToken cancellationToken)
    {
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .Include(x => x.IncidentType)
            .Include(x => x.Locations)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (incident is null)
        {
            return NotFound();
        }

        var environment = await _dbContext.IncidentEnvironmentalContexts
            .AsNoTracking()
            .Where(x => x.IncidentId == id)
            .OrderByDescending(x => x.ObservedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var prediction = await _dbContext.PredictiveAnalyses
            .AsNoTracking()
            .Where(x => x.IncidentId == id)
            .OrderByDescending(x => x.GeneratedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var visual = await _dbContext.VisualAnalyses
            .AsNoTracking()
            .Where(x => x.IncidentId == id && x.FindingsJson != null)
            .OrderByDescending(x => x.CompletedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        VisualContext? visualContext = null;
        if (visual?.FindingsJson is not null)
        {
            try
            {
                var findings = JsonSerializer.Deserialize<VisualContext>(
                    visual.FindingsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                visualContext = findings;
            }
            catch (JsonException)
            {
                visualContext = null;
            }
        }

        var location = incident.Locations.FirstOrDefault(x => x.IsPrimary)
            ?? incident.Locations.FirstOrDefault();
        var alertTypes = environment?.SevereWeatherAlertTypesJson is null
            ? []
            : JsonSerializer.Deserialize<IReadOnlyList<string>>(
                environment.SevereWeatherAlertTypesJson) ?? [];

        return Ok(new IncidentContextSnapshot
        {
            Incident = new IncidentContextIncident
            {
                IncidentType = incident.IncidentType.Name,
                Title = incident.Title,
                Description = incident.Description,
                Latitude = location is null ? 0 : (double)location.Latitude,
                Longitude = location is null ? 0 : (double)location.Longitude,
                PeopleAffected = incident.PeopleAffected,
                VehiclesAffected = incident.VehiclesAffected,
                Casualties = incident.Casualties,
                SeverityScore = incident.SeverityScore,
                PriorityScore = incident.PriorityScore
            },
            Visual = visualContext,
            Environment = environment is null
                ? null
                : new EnvironmentalContext
                {
                    TemperatureC = environment.TemperatureC,
                    WindSpeedKph = environment.WindSpeedKph,
                    PrecipitationMm = environment.PrecipitationMm,
                    VisibilityKm = environment.VisibilityKm,
                    RelativeHumidity = environment.RelativeHumidity,
                    HasSevereWeatherAlert = environment.HasSevereWeatherAlert,
                    SevereWeatherTypes = alertTypes,
                    AirQualityIndex = environment.AirQualityIndex
                },
            PredictionProbability = prediction?.Probability,
            PredictionModelVersion = prediction?.ModelVersion,
            PredictionFeatureSchemaVersion = prediction?.FeatureSchemaVersion
        });
    }

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpGet("{incidentId:guid}/duplicates")]
    [ServiceFilter(typeof(AIEnabledActionFilter))]
    public async Task<
        ActionResult<ApiResponse<IReadOnlyList<DuplicateIncidentCandidate>>>>
        GetDuplicates(
            Guid incidentId,
            CancellationToken cancellationToken)
    {
        var source = await _dbContext.Incidents.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == incidentId, cancellationToken);
        if (source is null) return NotFound();
        if (source.Embedding is null)
        {
            return Conflict(new { message = "Duplicate assessment is unavailable because the incident has no embedding. No duplicate conclusion has been made." });
        }

        var candidates =
            await _duplicateIncidentService.FindCandidatesAsync(
                incidentId,
                cancellationToken: cancellationToken);

        return Ok(
            ApiResponse<IReadOnlyList<DuplicateIncidentCandidate>>.Ok(
                candidates));
    }

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpPost("{incidentId:guid}/duplicates/{matchId:guid}/review")]
    public async Task<
        ActionResult<ApiResponse<IncidentDuplicateMatch>>>
        ReviewDuplicate(
            Guid incidentId,
            Guid matchId,
            ReviewDuplicateIncidentRequest request,
            CancellationToken cancellationToken)
    {
        var match =
            await _dbContext.IncidentDuplicateMatches
                .SingleOrDefaultAsync(
                    x =>
                        x.Id == matchId &&
                        x.SourceIncidentId == incidentId,
                    cancellationToken);

        if (match is null)
        {
            return NotFound();
        }

        if (match.Reviewed)
        {
            return Conflict(
                "This duplicate match has already been reviewed.");
        }

        var incident =
            await _dbContext.Incidents
                .SingleOrDefaultAsync(
                    x => x.Id == incidentId,
                    cancellationToken);

        if (incident is null)
        {
            return NotFound();
        }

        if (request.ConfirmedDuplicate &&
            request.CanonicalIncidentId is null)
        {
            return BadRequest(
                "A canonical incident is required when confirming a duplicate.");
        }

        if (request.ConfirmedDuplicate &&
            request.CanonicalIncidentId != match.SourceIncidentId &&
            request.CanonicalIncidentId != match.CandidateIncidentId)
        {
            return BadRequest(
                "The canonical incident must be one of the reviewed incidents.");
        }

        if (request.ConfirmedDuplicate)
        {
            var canonicalIncidentId =
                request.CanonicalIncidentId!.Value;

            var duplicateIncidentId =
                canonicalIncidentId == match.SourceIncidentId
                    ? match.CandidateIncidentId
                    : match.SourceIncidentId;

            await _incidentConsolidationService
                .ConsolidateDuplicateAsync(
                    canonicalIncidentId,
                    duplicateIncidentId,
                    request.ReviewDecision,
                    cancellationToken);

            _dbContext.AuditLogs.Add(
                new AuditLog
                {
                    UserId = _currentUserService.UserId,
                    Action = "IncidentConsolidated",
                    EntityName = nameof(IncidentRelation),
                    EntityId = duplicateIncidentId.ToString(),
                    NewValues = JsonSerializer.Serialize(
                        new
                        {
                            canonicalIncidentId,
                            duplicateIncidentId,
                            reviewer = _currentUserService.UserId,
                            decision = "Duplicate confirmed",
                            notes = request.ReviewDecision
                        }),
                    OccurredAtUtc = DateTime.UtcNow
                });
        }

        match.Reviewed = true;
        match.ConfirmedDuplicate = request.ConfirmedDuplicate;
        match.ReviewDecision = request.ReviewDecision;
        match.ReviewedByUserId = _currentUserService.UserId;
        match.ReviewedAtUtc = DateTime.UtcNow;

        _dbContext.AuditLogs.Add(
            new AuditLog
            {
                UserId = _currentUserService.UserId,
                Action = "DuplicateIncidentReviewed",
                EntityName = nameof(IncidentDuplicateMatch),
                EntityId = match.Id.ToString(),
                NewValues = JsonSerializer.Serialize(
                    new
                    {
                        sourceIncidentId = match.SourceIncidentId,
                        candidateIncidentId = match.CandidateIncidentId,
                        confirmedDuplicate = match.ConfirmedDuplicate,
                        decision = match.ReviewDecision
                    }),
                OccurredAtUtc = DateTime.UtcNow
            });

        await _dbContext.SaveChangesAsync(cancellationToken);

        var hasDuplicateCandidates =
            await _dbContext.IncidentDuplicateMatches
                .AnyAsync(
                    x =>
                        x.SourceIncidentId == incident.Id &&
                        !x.Reviewed &&
                        x.DuplicateScore >= 0.75,
                    cancellationToken);

        await _notificationService
            .NotifyIncidentUpdatedAsync(
                new IncidentUpdatedEvent
                {
                    IncidentId = incident.Id,
                    IncidentNumber = incident.IncidentNumber,
                    Status = incident.Status.ToString(),
                    HasDuplicateCandidates = hasDuplicateCandidates,
                    DuplicateDetectionStatus =
                        incident.DuplicateDetectionStatus,
                    Severity = incident.Severity.ToString(),
                    SeverityScore = incident.SeverityScore,
                    Priority = incident.Priority.ToString(),
                    PriorityScore = incident.PriorityScore,
                    UpdateType = "DUPLICATE_REVIEWED"
                },
                cancellationToken);

        await _notificationService.NotifyDashboardUpdatedAsync(
            new DashboardUpdatedEvent
            {
                Reason = DashboardUpdateReasons.IncidentReviewed,
                IncidentId = incidentId,
                UpdatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        return Ok(
            ApiResponse<IncidentDuplicateMatch>.Ok(match));
    }


    [Authorize(Policy = "OperatorAccess")]
    [HttpGet("{incidentId:guid}/media")]
    public async Task<ActionResult<IReadOnlyList<IncidentMediaResponse>>> GetMediaList(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        var mediaItems = await _dbContext.IncidentMedia
            .AsNoTracking()
            .Where(x => x.IncidentId == incidentId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var latestAnalyses = await _dbContext.VisualAnalyses
            .AsNoTracking()
            .Where(x => x.IncidentId == incidentId)
            .GroupBy(x => x.IncidentMediaId)
            .Select(g => g.OrderByDescending(x => x.CreatedAtUtc).First())
            .ToListAsync(cancellationToken);

        var analysisByMediaId = latestAnalyses
            .ToDictionary(x => x.IncidentMediaId);

        var result = mediaItems.Select(m =>
        {
            analysisByMediaId.TryGetValue(m.Id, out var analysis);

            return new IncidentMediaResponse
            {
                Id = m.Id,
                IncidentId = m.IncidentId,
                ContentType = m.ContentType,
                SizeBytes = m.SizeBytes,
                CreatedAtUtc = m.CreatedAtUtc,
                AIAnalysisRequested = m.AIAnalysisRequested,
                VisualAnalysisStatus = analysis?.Status,
                VisualAnalysisConfidence = analysis?.Confidence,
                MediaUrl =
                    $"/api/incidents/{incidentId}/media/{m.Id}"
            };
        }).ToList();

        return Ok(result);
    }

    [Authorize(Policy = "OperatorAccess")]
    [HttpGet("{incidentId:guid}/media/{mediaId:guid}")]
    public async Task<IActionResult> GetMedia(
        Guid incidentId,
        Guid mediaId,
        CancellationToken cancellationToken)
    {
        var media = await _dbContext.IncidentMedia
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == mediaId && x.IncidentId == incidentId,
                cancellationToken);

        if (media is null)
        {
            return NotFound();
        }

        var isOperator =
            _currentUserService.IsInRole(ApplicationRoles.Operator) ||
            _currentUserService.IsInRole(ApplicationRoles.EmergencyAdmin) ||
            _currentUserService.IsInRole(ApplicationRoles.SystemAdmin);

        if (!isOperator)
        {
            return Forbid();
        }

        _dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            UserId = _currentUserService.UserId,
            Action = SecurityEventTypes.MediaAccess,
            EntityName = nameof(IncidentMedia),
            EntityId = media.Id.ToString(),
            OccurredAtUtc = DateTime.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        var stream = await _mediaStorage.OpenReadAsync(
            media.StorageKey,
            cancellationToken);

        return File(
            stream,
            media.ContentType,
            enableRangeProcessing: true);
    }

    /// <summary>
    /// Returns a single extracted video frame JPEG for a completed visual analysis.
    /// frameNumber is 0-based and must be within [0, FramesAnalyzed).
    /// </summary>
    [Authorize(Policy = "OperatorAccess")]
    [HttpGet("{incidentId:guid}/media/{mediaId:guid}/analysis/frames/{frameNumber:int}")]
    public async Task<IActionResult> GetAnalysisFrame(
        Guid incidentId,
        Guid mediaId,
        int frameNumber,
        CancellationToken cancellationToken)
    {
        if (frameNumber < 0)
        {
            return BadRequest("Frame number must be non-negative.");
        }

        // Load the latest analysis for this media item.
        var analysis = await _dbContext.VisualAnalyses
            .AsNoTracking()
            .Where(x =>
                x.IncidentId == incidentId &&
                x.IncidentMediaId == mediaId &&
                x.FrameStorageKeysJson != null)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (analysis is null)
        {
            return NotFound("No frame data found for this media item.");
        }

        List<string>? keys;

        try
        {
            keys = System.Text.Json.JsonSerializer
                .Deserialize<List<string>>(analysis.FrameStorageKeysJson!);
        }
        catch
        {
            return NotFound("Frame storage metadata is corrupted.");
        }

        if (keys is null || frameNumber >= keys.Count)
        {
            return NotFound(
                $"Frame {frameNumber} does not exist. " +
                $"This analysis has {keys?.Count ?? 0} frame(s).");
        }

        var storageKey = keys[frameNumber];

        Stream frameStream;

        try
        {
            frameStream = await _mediaStorage.OpenReadAsync(
                storageKey,
                cancellationToken);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Frame image file was not found in storage.");
        }

        return File(frameStream, "image/jpeg");
    }

    [Authorize]
    [HttpPost("{incidentId:guid}/media")]
    [EnableRateLimiting("media-upload")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadMedia(
        Guid incidentId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == incidentId,
                cancellationToken);

        if (incident is null)
        {
            return NotFound();
        }

        await using var stream = file.OpenReadStream();

        var mediaId = await _incidentMediaService.SaveImageAsync(
            incidentId,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);

        if (_aiEnabled)
        {
            await _visualAnalysisQueue.EnqueueAsync(
                new VisualAnalysisJob { IncidentMediaId = mediaId, IsVideo = false },
                cancellationToken);
        }

        return Ok(new { mediaId, analysisStatus = _aiEnabled ? "Queued" : "Disabled" });
    }

    [Authorize]
    [HttpPost("{incidentId:guid}/media/video")]
    [EnableRateLimiting("media-upload")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> UploadVideo(
        Guid incidentId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var incident = await _dbContext.Incidents
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == incidentId,
                cancellationToken);

        if (incident is null)
        {
            return NotFound();
        }

        await using var stream = file.OpenReadStream();

        var mediaId = await _incidentMediaService.SaveVideoAsync(
            incidentId,
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            cancellationToken);

        if (_aiEnabled)
        {
            await _visualAnalysisQueue.EnqueueAsync(
                new VisualAnalysisJob { IncidentMediaId = mediaId, IsVideo = true },
                cancellationToken);
        }

        return Ok(new { mediaId, analysisStatus = _aiEnabled ? "Queued" : "Disabled" });
    }

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpGet("{incidentId:guid}/media/{mediaId:guid}/analysis")]
    public async Task<ActionResult<VisualAnalysisResponse>> GetMediaAnalysis(
        Guid incidentId,
        Guid mediaId,
        CancellationToken cancellationToken)
    {
        var analysis = await _dbContext.VisualAnalyses
            .AsNoTracking()
            .Where(x =>
                x.IncidentId == incidentId &&
                x.IncidentMediaId == mediaId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (analysis is null)
        {
            return NotFound();
        }

        var findings = analysis.FindingsJson is not null
            ? JsonSerializer.Deserialize<VisualAnalysisResult>(
                analysis.FindingsJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
            : null;

        return Ok(new VisualAnalysisResponse
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
            HumanDecision = analysis.HumanDecision
        });
    }

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpGet("{incidentId:guid}/media/{mediaId:guid}/analysis/review")]
    public async Task<ActionResult<VisualAnalysisResponse>> GetVisualReview(
        Guid incidentId,
        Guid mediaId,
        CancellationToken cancellationToken)
    {
        var analysis = await _dbContext.VisualAnalyses
            .AsNoTracking()
            .Where(x =>
                x.IncidentId == incidentId &&
                x.IncidentMediaId == mediaId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (analysis is null)
        {
            return NotFound();
        }

        var findings = analysis.FindingsJson is not null
            ? JsonSerializer.Deserialize<VisualAnalysisResult>(
                analysis.FindingsJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })
            : null;

        return Ok(new VisualAnalysisResponse
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
            HumanDecision = analysis.HumanDecision
        });
    }

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpPost("{incidentId:guid}/media/{mediaId:guid}/analysis/review")]
    public async Task<IActionResult> SubmitVisualReview(
        Guid incidentId,
        Guid mediaId,
        ReviewVisualAnalysisRequest request,
        CancellationToken cancellationToken)
    {
        var analysis = await _dbContext.VisualAnalyses
            .Where(x =>
                x.IncidentId == incidentId &&
                x.IncidentMediaId == mediaId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (analysis is null)
        {
            return NotFound();
        }

        var oldStatus = analysis.Status;

        analysis.HumanReviewed = true;
        analysis.HumanDecision = request.Decision;
        analysis.Status = request.Accepted
            ? VisualAnalysisStatus.Completed
            : VisualAnalysisStatus.Rejected;

        _dbContext.AuditLogs.Add(new AuditLog
        {
            UserId = _currentUserService.UserId,
            Action = "VisualAnalysisReviewed",
            EntityName = nameof(VisualAnalysis),
            EntityId = analysis.Id.ToString(),
            OldValues = JsonSerializer.Serialize(new
            {
                status = oldStatus.ToString(),
                humanReviewed = false
            }),
            NewValues = JsonSerializer.Serialize(new
            {
                status = analysis.Status.ToString(),
                humanReviewed = true,
                accepted = request.Accepted,
                decision = request.Decision
            }),
            OccurredAtUtc = DateTime.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _notificationService.NotifyDashboardUpdatedAsync(
            new DashboardUpdatedEvent
            {
                Reason = DashboardUpdateReasons.IncidentReviewed,
                IncidentId = incidentId,
                UpdatedAtUtc = DateTime.UtcNow
            },
            cancellationToken);

        return NoContent();
    }

    [Authorize(Policy = "OperatorAccess")]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<IncidentResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateIncidentStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            return BadRequest("Status is required.");
        }

        try
        {
            var result = await _incidentService.UpdateStatusAsync(
                id,
                request,
                cancellationToken);

            if (result is null)
            {
                return NotFound();
            }

            await _notificationService.NotifyDashboardUpdatedAsync(
                new DashboardUpdatedEvent
                {
                    Reason = DashboardUpdateReasons.IncidentUpdated,
                    IncidentId = id,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                cancellationToken);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult<PagedResult<IncidentListItem>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] Guid? incidentTypeId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? severity = null,
        [FromQuery] bool myReportsOnly = false,
        CancellationToken cancellationToken = default)
    {
        var isPrivileged =
            _currentUserService.IsInRole(ApplicationRoles.Operator) ||
            _currentUserService.IsInRole(ApplicationRoles.EmergencyAdmin) ||
            _currentUserService.IsInRole(ApplicationRoles.SystemAdmin);

        var result = (isPrivileged && !myReportsOnly)
            ? await _incidentService.GetPagedAsync(
                page,
                pageSize,
                search,
                incidentTypeId,
                status,
                severity,
                cancellationToken)
            : await _incidentService.GetPagedForCurrentUserAsync(
                page,
                pageSize,
                search,
                incidentTypeId,
                status,
                severity,
                cancellationToken);

        return Ok(result);
    }

    [Authorize(Roles = $"{ApplicationRoles.Responder},{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    [HttpGet("{id:guid}/predictions/escalation-risk")]
    public async Task<ActionResult<EscalationRiskPrediction>> GetEscalationRiskPrediction(
        Guid id,
        CancellationToken cancellationToken)
    {
        var incidentExists = await _dbContext.Incidents
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);

        if (!incidentExists)
        {
            return NotFound($"Incident {id} not found.");
        }

        var prediction = (await _predictionService
            .GetHistoryAsync(id, cancellationToken))
            .FirstOrDefault();

        return prediction is null
            ? NotFound($"No escalation-risk prediction exists for incident {id}.")
            : Ok(prediction);
    }

    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    [HttpPost("{id:guid}/predictions/reprocess")]
    public async Task<IActionResult> ReprocessPrediction(
        Guid id,
        CancellationToken cancellationToken)
    {
        var incidentExists = await _dbContext.Incidents
            .AsNoTracking()
            .AnyAsync(x => x.Id == id, cancellationToken);

        if (!incidentExists)
        {
            return NotFound($"Incident {id} not found.");
        }

        if (_predictiveAnalysisQueue is null)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                "Predictive analysis is not available.");
        }

        await _predictiveAnalysisQueue.EnqueueAsync(
            new PredictiveAnalysisJob(id, "ManualReprocess"),
            cancellationToken);

        return Accepted(new
        {
            message = $"Predictive analysis re-processing enqueued for incident {id}."
        });
    }

    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    [HttpPost("{id:guid}/ai/reprocess")]
    [ServiceFilter(typeof(AIEnabledActionFilter))]
    public async Task<IActionResult> ReprocessAI(
        Guid id,
        CancellationToken cancellationToken)
    {
        var incident = await _dbContext.Incidents.FindAsync(new object[] { id }, cancellationToken);
        if (incident is null)
        {
            return NotFound($"Incident {id} not found.");
        }

        if (_aiAnalysisQueue is not null)
        {
            await _aiAnalysisQueue.EnqueueAsync(new IncidentAIJob(id), cancellationToken);
        }

        return Accepted(new { message = $"AI re-processing enqueued for incident {id}." });
    }

    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    [HttpPost("{id:guid}/duplicates/reprocess")]
    [ServiceFilter(typeof(AIEnabledActionFilter))]
    public async Task<IActionResult> ReprocessDuplicates(
        Guid id,
        CancellationToken cancellationToken)
    {
        var incident = await _dbContext.Incidents.FindAsync(new object[] { id }, cancellationToken);
        if (incident is null)
        {
            return NotFound($"Incident {id} not found.");
        }

        incident.DuplicateDetectionStatus = DuplicateDetectionStatus.Processing;
        incident.DuplicateDetectionFailureReason = null;
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            await _duplicateIncidentService.GenerateEmbeddingAsync(id, cancellationToken);
            await _duplicateIncidentService.FindCandidatesAsync(id, cancellationToken: cancellationToken);

            incident.DuplicateDetectionStatus = DuplicateDetectionStatus.Completed;
            incident.DuplicateDetectionCompletedAtUtc = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _notificationService.NotifyDashboardUpdatedAsync(
                new DashboardUpdatedEvent
                {
                    Reason = DashboardUpdateReasons.DuplicateDetected,
                    IncidentId = id,
                    UpdatedAtUtc = DateTime.UtcNow
                },
                cancellationToken);

            return Ok(new { message = $"Duplicate detection reprocessed for incident {id}." });
        }
        catch (Exception ex)
        {
            incident.DuplicateDetectionStatus = DuplicateDetectionStatus.Failed;
            incident.DuplicateDetectionFailureReason = ex.Message;
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new { message = $"Duplicate detection reprocessed with warnings: {ex.Message}", status = "Failed" });
        }
    }

    [Authorize(Roles = $"{ApplicationRoles.Operator},{ApplicationRoles.EmergencyAdmin},{ApplicationRoles.SystemAdmin}")]
    [HttpPost("{id:guid}/visual/reprocess")]
    [ServiceFilter(typeof(AIEnabledActionFilter))]
    public async Task<IActionResult> ReprocessVisual(
        Guid id,
        CancellationToken cancellationToken)
    {
        var incident = await _dbContext.Incidents.FindAsync(new object[] { id }, cancellationToken);
        if (incident is null)
        {
            return NotFound($"Incident {id} not found.");
        }

        var mediaList = await _dbContext.IncidentMedia
            .Where(m => m.IncidentId == id)
            .ToListAsync(cancellationToken);

        if (mediaList.Count == 0)
        {
            return NotFound($"No media found for incident {id}.");
        }

        foreach (var media in mediaList)
        {
            await _visualAnalysisQueue.EnqueueAsync(
                new VisualAnalysisJob { IncidentMediaId = media.Id, IsVideo = media.MediaType == IncidentMediaType.Video },
                cancellationToken);
        }

        return Accepted(new { message = $"Visual analysis re-processing enqueued for {mediaList.Count} media items." });
    }
}
