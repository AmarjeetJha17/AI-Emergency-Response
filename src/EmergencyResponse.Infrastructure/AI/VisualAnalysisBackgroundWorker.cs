using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Observability;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class VisualAnalysisBackgroundWorker : BackgroundService
{
    private readonly IVisualAnalysisQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<VisualAnalysisBackgroundWorker> _logger;
    private readonly IWorkerHealthState? _workerHealthState;

    public VisualAnalysisBackgroundWorker(
        IVisualAnalysisQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<VisualAnalysisBackgroundWorker> logger,
        IWorkerHealthState? workerHealthState = null)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _workerHealthState = workerHealthState;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Visual analysis background worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var job = default(Application.AI.Jobs.VisualAnalysisJob);

            try
            {
                job = await _queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            using var activity = EmergencyResponseTelemetry.ActivitySource.StartActivity("AI.VisualAnalysis");
            activity?.SetTag("incident.media_id", job.IncidentMediaId.ToString());
            var timer = Stopwatch.StartNew();

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<ApplicationDbContext>();

                var media = await db.IncidentMedia
                    .SingleOrDefaultAsync(
                        x => x.Id == job.IncidentMediaId,
                        stoppingToken);

                if (media is null)
                {
                    _logger.LogWarning(
                        "IncidentMedia {MediaId} no longer exists.",
                        job.IncidentMediaId);

                    continue;
                }

                var orchestrator = scope.ServiceProvider
                    .GetRequiredService<
                        VisualAnalysisOrchestrationService>();

                var analysis = await orchestrator.AnalyzeAndPersistAsync(
                    job.IncidentMediaId,
                    job.IsVideo,
                    stoppingToken);

                timer.Stop();
                EmergencyResponseTelemetry.VisualAnalysisDuration.Record(timer.Elapsed.TotalMilliseconds);
                _workerHealthState?.MarkSuccess("VisualAnalysisWorker");

                var notificationService = scope.ServiceProvider
                    .GetRequiredService<IIncidentNotificationService>();

                await notificationService.NotifyIncidentUpdatedAsync(
                    new IncidentUpdatedEvent
                    {
                        IncidentId = media.IncidentId,
                        IncidentMediaId = job.IncidentMediaId,
                        VisualAnalysisAvailable = true,
                        VisualAnalysisStatus = analysis.Status,
                        UpdateType = "VISUAL_ANALYSIS_COMPLETED"
                    },
                    stoppingToken);

                await notificationService.NotifyDashboardUpdatedAsync(
                    new DashboardUpdatedEvent
                    {
                        Reason = DashboardUpdateReasons.VisualAnalysisCompleted,
                        IncidentId = media.IncidentId,
                        UpdatedAtUtc = DateTime.UtcNow
                    },
                    stoppingToken);

                _logger.LogInformation(
                    "Visual analysis completed for media {MediaId}. Status={Status}.",
                    job.IncidentMediaId,
                    analysis.Status);
            }
            catch (Exception ex)
            {
                timer.Stop();
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                _logger.LogError(
                    ex,
                    "Visual analysis failed for media {MediaId}.",
                    job?.IncidentMediaId);

                if (job is not null)
                {
                    await PersistFailureAsync(
                        job.IncidentMediaId,
                        ex,
                        stoppingToken);

                    try
                    {
                        using var failureScope = _scopeFactory.CreateScope();
                        var db = failureScope.ServiceProvider
                            .GetRequiredService<ApplicationDbContext>();

                        var incidentId = await db.IncidentMedia
                            .Where(x => x.Id == job.IncidentMediaId)
                            .Select(x => x.IncidentId)
                            .FirstOrDefaultAsync(stoppingToken);

                        if (incidentId != Guid.Empty)
                        {
                            var notificationService = failureScope.ServiceProvider
                                .GetRequiredService<IIncidentNotificationService>();

                            await notificationService.NotifyIncidentUpdatedAsync(
                                new IncidentUpdatedEvent
                                {
                                    IncidentId = incidentId,
                                    IncidentMediaId = job.IncidentMediaId,
                                    VisualAnalysisAvailable = true,
                                    VisualAnalysisStatus = VisualAnalysisStatus.Failed,
                                    UpdateType = "VISUAL_ANALYSIS_FAILED"
                                },
                                stoppingToken);

                            await notificationService.NotifyDashboardUpdatedAsync(
                                new DashboardUpdatedEvent
                                {
                                    Reason = DashboardUpdateReasons.VisualAnalysisCompleted,
                                    IncidentId = incidentId,
                                    UpdatedAtUtc = DateTime.UtcNow
                                },
                                stoppingToken);
                        }
                    }
                    catch (Exception notifEx)
                    {
                        _logger.LogWarning(
                            notifEx,
                            "Failed to publish failure notification for media {MediaId}.",
                            job.IncidentMediaId);
                    }
                }
            }
        }

        _logger.LogInformation(
            "Visual analysis background worker stopped.");
    }

    private async Task PersistFailureAsync(
        Guid incidentMediaId,
        Exception exception,
        CancellationToken cancellationToken)
    {
        try
        {
            var reason = exception.Message;
            var category = CategorizeFailure(exception);

            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var existing = await db.VisualAnalyses
                .FirstOrDefaultAsync(
                    x => x.IncidentMediaId == incidentMediaId,
                    cancellationToken);

            if (existing is not null)
            {
                existing.Status = VisualAnalysisStatus.Failed;
                existing.FailureReason =
                    reason.Length <= 2000 ? reason : reason[..2000];
                existing.FailureCategory = category;
                existing.LastAttemptAtUtc = DateTime.UtcNow;
                existing.RetryCount++;
                existing.NextRetryAtUtc = DateTime.UtcNow.AddSeconds(
                    Math.Min(300, Math.Pow(2, existing.RetryCount) * 5));

                await db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                var media = await db.IncidentMedia
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Id == incidentMediaId,
                        cancellationToken);

                if (media is not null)
                {
                    db.VisualAnalyses.Add(new Domain.Entities.VisualAnalysis
                    {
                        Id = Guid.NewGuid(),
                        IncidentMediaId = media.Id,
                        IncidentId = media.IncidentId,
                        ModelName = "gpt-5-mini",
                        ModelVersion = "gpt-5-mini",
                        PromptVersion = Application.AI.Common.AIPromptVersions.VisualEvidenceAnalysis,
                        Status = VisualAnalysisStatus.Failed,
                        FailureReason = reason.Length <= 2000 ? reason : reason[..2000],
                        FailureCategory = category,
                        AttemptNumber = 1,
                        RetryCount = 1,
                        LastAttemptAtUtc = DateTime.UtcNow,
                        CreatedAtUtc = DateTime.UtcNow
                    });

                    await db.SaveChangesAsync(cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to persist visual analysis failure for media {MediaId}.",
                incidentMediaId);
        }
    }

    private static string CategorizeFailure(Exception ex)
    {
        if (ex is TaskCanceledException or OperationCanceledException or TimeoutException)
        {
            return "Timeout";
        }
        if (ex is HttpRequestException httpEx)
        {
            if (httpEx.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                return "RateLimited";
            if ((int?)httpEx.StatusCode >= 500)
                return "ProviderUnavailable";
            return "TransientNetwork";
        }
        if (ex is JsonException)
        {
            return "SchemaValidation";
        }
        return "Unknown";
    }
}
