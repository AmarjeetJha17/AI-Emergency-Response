using System.Text.Json;

using EmergencyResponse.Api.Common;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.DuplicateDetection;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.DTOs.Common;
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

    public IncidentsController(
        IIncidentService incidentService,
        IDuplicateIncidentService duplicateIncidentService,
        IIncidentConsolidationService incidentConsolidationService,
        ApplicationDbContext dbContext,
        ICurrentUserService currentUserService,
        IIncidentNotificationService notificationService)
    {
        _incidentService = incidentService;
        _duplicateIncidentService = duplicateIncidentService;
        _incidentConsolidationService = incidentConsolidationService;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _notificationService = notificationService;
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