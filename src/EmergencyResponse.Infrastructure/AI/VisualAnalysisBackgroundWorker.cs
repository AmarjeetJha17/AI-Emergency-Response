using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
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

    public VisualAnalysisBackgroundWorker(
        IVisualAnalysisQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<VisualAnalysisBackgroundWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
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

                _logger.LogInformation(
                    "Visual analysis completed for media {MediaId}. Status={Status}.",
                    job.IncidentMediaId,
                    analysis.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Visual analysis failed for media {MediaId}.",
                    job?.IncidentMediaId);

                if (job is not null)
                {
                    await PersistFailureAsync(
                        job.IncidentMediaId,
                        ex.Message,
                        stoppingToken);
                }
            }
        }

        _logger.LogInformation(
            "Visual analysis background worker stopped.");
    }

    private async Task PersistFailureAsync(
        Guid incidentMediaId,
        string reason,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var existing = await db.VisualAnalyses
                .FirstOrDefaultAsync(
                    x => x.IncidentMediaId == incidentMediaId &&
                         x.Status == VisualAnalysisStatus.Processing,
                    cancellationToken);

            if (existing is not null)
            {
                existing.Status = VisualAnalysisStatus.Failed;
                existing.FailureReason =
                    reason.Length <= 2000 ? reason : reason[..2000];

                await db.SaveChangesAsync(cancellationToken);
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
}
