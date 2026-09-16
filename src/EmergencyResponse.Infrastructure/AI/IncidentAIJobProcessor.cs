using System.Text.Json;

using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Decision.Interfaces;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class IncidentAIJobProcessor
    : IIncidentAIJobProcessor
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IIncidentAnalysisService _analysisService;
    private readonly IIncidentDecisionService _decisionService;
    private readonly AIOptions _aiOptions;
    private readonly ILogger<IncidentAIJobProcessor> _logger;
    private readonly IAIReviewPolicy _reviewPolicy;
    private readonly IDuplicateIncidentService _duplicateIncidentService;
    private readonly IIncidentAIOrchestrationService _aiOrchestrationService;
    private readonly IIncidentNotificationService _notificationService;
    private readonly IServiceScopeFactory _scopeFactory;

    public IncidentAIJobProcessor(
        IServiceScopeFactory scopeFactory,
        ApplicationDbContext dbContext,
        IIncidentAIOrchestrationService aiOrchestrationService,
        IIncidentAnalysisService analysisService,
        IIncidentDecisionService decisionService,
        IOptions<AIOptions> aiOptions,
        ILogger<IncidentAIJobProcessor> logger,
        IAIReviewPolicy reviewPolicy,
        IIncidentNotificationService notificationService,
        IDuplicateIncidentService duplicateIncidentService)
    {
        _scopeFactory = scopeFactory;
        _dbContext = dbContext;
        _aiOrchestrationService = aiOrchestrationService;
        _analysisService = analysisService;
        _decisionService = decisionService;
        _aiOptions = aiOptions.Value;
        _logger = logger;
        _reviewPolicy = reviewPolicy;
        _notificationService = notificationService;
        _duplicateIncidentService = duplicateIncidentService;
    }

    public async Task ProcessAsync(
        IncidentAIJob job,
        CancellationToken cancellationToken = default)
    {
        var incident =
            await _dbContext.Incidents
                .Include(x => x.IncidentType)
                .Include(x => x.Locations)
                .FirstOrDefaultAsync(
                    x => x.Id == job.IncidentId,
                    cancellationToken);

        if (incident is null)
        {
            _logger.LogWarning(
                "Incident {IncidentId} no longer exists.",
                job.IncidentId);

            return;
        }

        var analysis = new AIAnalysis
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            Status = AIAnalysisStatus.Processing,
            ModelName = _aiOptions.Model,
            ModelVersion = "unknown",
            PromptVersion = "IncidentAnalysis_v1",
            InputReference = $"Incident:{incident.Id}",
            OutputJson = "{}",
            HumanReviewed = false
        };

        _dbContext.AIAnalyses.Add(analysis);

        await _dbContext.SaveChangesAsync(
            cancellationToken);

        try
        {
            incident.DuplicateDetectionStatus =
                DuplicateDetectionStatus.Processing;

            incident.DuplicateDetectionFailureReason = null;

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            try
            {
                await _duplicateIncidentService.GenerateEmbeddingAsync(
                    incident.Id,
                    cancellationToken);

                await _duplicateIncidentService.FindCandidatesAsync(
                    incident.Id,
                    cancellationToken: cancellationToken);

                incident.DuplicateDetectionStatus =
                    DuplicateDetectionStatus.Completed;

                incident.DuplicateDetectionCompletedAtUtc =
                    DateTime.UtcNow;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }
            catch (Exception ex)
            {
                incident.DuplicateDetectionStatus =
                    DuplicateDetectionStatus.Failed;

                incident.DuplicateDetectionFailureReason =
                    ex.Message;

                await _dbContext.SaveChangesAsync(
                    cancellationToken);

                _logger.LogError(
                    ex,
                    "Duplicate detection failed for incident {IncidentId}",
                    incident.Id);
            }

            var location =
                incident.Locations
                    .FirstOrDefault(x => x.IsPrimary)
                ?? incident.Locations.FirstOrDefault();

            var input =
                new IncidentAnalysisInput
                {
                    Title = incident.Title,
                    Description = incident.Description,
                    IncidentType =
                        incident.IncidentType.Name,
                    Latitude = location?.Latitude ?? 0,
                    Longitude = location?.Longitude ?? 0,
                    Address = location?.Address,
                    City = location?.City,
                    District = location?.District,
                    State = location?.State,
                    PeopleAffected =
                        incident.PeopleAffected,
                    VehiclesAffected =
                        incident.VehiclesAffected,
                    Casualties =
                        incident.Casualties
                };

            var started =
                System.Diagnostics.Stopwatch.StartNew();

            var aiResult =
                await _analysisService.AnalyzeAsync(
                    input,
                    cancellationToken);

            started.Stop();

            var decision =
                _decisionService.Evaluate(
                    aiResult);

            var requiresReview =
                _reviewPolicy.RequiresHumanReview(
                    aiResult);

            analysis.ExtractedFactsJson =
                JsonSerializer.Serialize(
                    aiResult.Facts);

            analysis.Status =
                requiresReview
                    ? AIAnalysisStatus.RequiresReview
                    : AIAnalysisStatus.Completed;

            analysis.OutputJson =
                JsonSerializer.Serialize(aiResult);

            analysis.Confidence =
                aiResult.Confidence;

            analysis.ExecutionTimeMs =
                started.ElapsedMilliseconds;

            incident.Severity =
                decision.Severity.Severity;

            incident.SeverityScore =
                decision.Severity.Score;

            incident.SeverityReasonsJson =
                JsonSerializer.Serialize(
                    decision.Severity.Reasons);

            incident.Priority =
                decision.Priority.Priority;

            incident.PriorityScore =
                decision.Priority.Score;

            incident.PriorityReasonsJson =
                JsonSerializer.Serialize(
                    decision.Priority.Reasons);

            await _dbContext.SaveChangesAsync(
                cancellationToken);

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
                        HasDuplicateCandidates =
                            hasDuplicateCandidates,
                        DuplicateDetectionStatus =
                            incident.DuplicateDetectionStatus,
                        Severity = incident.Severity.ToString(),
                        SeverityScore = incident.SeverityScore,
                        Priority = incident.Priority.ToString(),
                        PriorityScore = incident.PriorityScore,
                        UpdateType = "AI_ANALYSIS_COMPLETED"
                    },
                    cancellationToken);

            _logger.LogInformation(
                "AI processing completed for incident {IncidentId}. " +
                "Severity={Severity}, Priority={Priority}.",
                incident.Id,
                incident.Severity,
                incident.Priority);
        }
        catch (Exception ex)
        {
            analysis.Status =
                AIAnalysisStatus.Failed;

            analysis.FailureReason =
                ex.Message.Length <= 2000
                    ? ex.Message
                    : ex.Message[..2000];

            await _dbContext.SaveChangesAsync(
                cancellationToken);

            _logger.LogError(
                ex,
                "AI analysis failed for incident {IncidentId}.",
                incident.Id);
        }
    }
}