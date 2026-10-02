using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Observability;
using EmergencyResponse.Application.Predictive;
using EmergencyResponse.Application.Predictive.Escalation;
using EmergencyResponse.Application.Predictive.Interfaces;
using EmergencyResponse.Application.Predictive.Jobs;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Predictive;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.Predictive;

public sealed class PredictiveAnalysisBackgroundWorker : BackgroundService
{
    private readonly IPredictiveAnalysisQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PredictiveAnalysisBackgroundWorker> _logger;
    private readonly IWorkerHealthState? _workerHealthState;

    public PredictiveAnalysisBackgroundWorker(
        IPredictiveAnalysisQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<PredictiveAnalysisBackgroundWorker> logger,
        IWorkerHealthState? workerHealthState = null)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _workerHealthState = workerHealthState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Predictive Analysis Background Worker started.");
        _workerHealthState?.MarkStarted(nameof(PredictiveAnalysisBackgroundWorker));

        while (!stoppingToken.IsCancellationRequested)
        {
            PredictiveAnalysisJob job;
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
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var featureBuilder = scope.ServiceProvider.GetRequiredService<IEscalationPredictionFeatureBuilder>();
                var predictionService = scope.ServiceProvider.GetRequiredService<IEscalationRiskPredictionService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<IIncidentNotificationService>();

                // Build snapshot and hash to check for duplicate/redundant inference
                var now = DateTime.UtcNow;
                var snapshot = await featureBuilder.BuildSnapshotAsync(job.IncidentId, now, stoppingToken);

                // Fetch latest prediction for this incident
                var latestPrediction = await dbContext.PredictiveAnalyses
                    .AsNoTracking()
                    .Where(p => p.IncidentId == job.IncidentId && p.PredictionType == PredictionType.EscalationRisk)
                    .OrderByDescending(p => p.GeneratedAtUtc)
                    .FirstOrDefaultAsync(stoppingToken);

                // Check feature fingerprint to skip redundant inference unless force reprocessed
                if (latestPrediction != null &&
                    job.TriggerReason != "ManualReprocess" &&
                    latestPrediction.Status != PredictionStatus.Failed &&
                    latestPrediction.ValidUntilUtc > now &&
                    !string.IsNullOrEmpty(latestPrediction.FeatureSnapshotJson))
                {
                    try
                    {
                        var lastSnapshot = System.Text.Json.JsonSerializer.Deserialize<PredictionFeatureSnapshot>(
                            latestPrediction.FeatureSnapshotJson);
                        if (lastSnapshot != null)
                        {
                            var lastHash = PredictionFeatureSnapshot.ComputeFeatureHash(lastSnapshot.Features);
                            if (string.Equals(lastHash, snapshot.FeatureHash, StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation(
                                    "Skipping redundant predictive analysis for incident {IncidentId}. Features unchanged (Hash: {Hash}).",
                                    job.IncidentId,
                                    snapshot.FeatureHash);

                                _workerHealthState?.MarkSuccess(nameof(PredictiveAnalysisBackgroundWorker));
                                continue;
                            }
                        }
                        else
                        {
                            var legacyFeatures = System.Text.Json.JsonSerializer
                                .Deserialize<EscalationPredictionFeatures>(
                                    latestPrediction.FeatureSnapshotJson);
                            if (legacyFeatures != null &&
                                string.Equals(
                                    PredictionFeatureSnapshot.ComputeFeatureHash(legacyFeatures),
                                    snapshot.FeatureHash,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                _logger.LogInformation(
                                    "Skipping redundant predictive analysis for legacy snapshot of incident {IncidentId}.",
                                    job.IncidentId);
                                _workerHealthState?.MarkSuccess(
                                    nameof(PredictiveAnalysisBackgroundWorker));
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to compare feature hash for incident {IncidentId}", job.IncidentId);
                    }
                }

                _logger.LogInformation(
                    "Executing predictive analysis for incident {IncidentId}. Trigger: {Reason}.",
                    job.IncidentId,
                    job.TriggerReason);

                var prediction = await predictionService.PredictAsync(job.IncidentId, stoppingToken);

                // Broadcast real-time update
                await notificationService.NotifyPredictionUpdatedAsync(
                    new PredictionUpdatedEvent
                    {
                        IncidentId = prediction.IncidentId,
                        PredictionType = "EscalationRisk",
                        PredictedEscalation = prediction.PredictedEscalation,
                        Probability = prediction.Probability,
                        RiskScore = prediction.RiskScore,
                        ModelName = prediction.ModelName,
                        ModelVersion = prediction.ModelVersion,
                        GeneratedAtUtc = prediction.GeneratedAtUtc
                    },
                    stoppingToken);

                _workerHealthState?.MarkSuccess(nameof(PredictiveAnalysisBackgroundWorker));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Predictive analysis worker failed for incident {IncidentId}",
                    job.IncidentId);
            }
        }

        _logger.LogInformation("Predictive Analysis Background Worker stopped.");
    }
}
