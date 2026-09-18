using System.Text.Json;

using EmergencyResponse.Api.Common;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.DuplicateDetection;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;
using EmergencyResponse.Application.AI.VisualAnalysis;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.DTOs.Common;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

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

    public IncidentsController(
        IIncidentService incidentService,
        IDuplicateIncidentService duplicateIncidentService,
        IIncidentConsolidationService incidentConsolidationService,
        ApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IIncidentNotificationService notificationService,
        IIncidentMediaService incidentMediaService,
        IVisualAnalysisQueue visualAnalysisQueue,
        IIncidentMediaStorage mediaStorage)
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
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<IncidentResponse>> Create(
        CreateIncidentRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _incidentService.CreateAsync(
                request,
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

    [Authorize(Roles = "Operator,EmergencyAdmin,SystemAdmin")]
    [HttpGet("{incidentId:guid}/duplicates")]
    public async Task<
        ActionResult<ApiResponse<IReadOnlyList<DuplicateIncidentCandidate>>>>
        GetDuplicates(
            Guid incidentId,
            CancellationToken cancellationToken)
    {
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

        await _visualAnalysisQueue.EnqueueAsync(
            new VisualAnalysisJob { IncidentMediaId = mediaId, IsVideo = false },
            cancellationToken);

        return Ok(new { mediaId });
    }

    [Authorize]
    [HttpPost("{incidentId:guid}/media/video")]
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

        await _visualAnalysisQueue.EnqueueAsync(
            new VisualAnalysisJob { IncidentMediaId = mediaId, IsVideo = true },
            cancellationToken);

        return Ok(new { mediaId });
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

        return NoContent();
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
        CancellationToken cancellationToken = default)
    {
        var result =
            await _incidentService.GetPagedForCurrentUserAsync(
                page,
                pageSize,
                search,
                incidentTypeId,
                status,
                severity,
                cancellationToken);

        return Ok(result);
    }
}