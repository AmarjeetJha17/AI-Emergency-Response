using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;
using EmergencyResponse.Application.Observability;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class IncidentAIBackgroundWorker
    : BackgroundService
{
    private readonly IAIAnalysisQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IncidentAIBackgroundWorker> _logger;
    private readonly IWorkerHealthState? _workerHealthState;

    public IncidentAIBackgroundWorker(
        IAIAnalysisQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<IncidentAIBackgroundWorker> logger,
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
            "Incident AI background worker started.");
        _workerHealthState?.MarkStarted("IncidentAIWorker");

        while (!stoppingToken.IsCancellationRequested)
        {
            IncidentAIJob job;

            try
            {
                job = await _queue.DequeueAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                using var scope =
                    _scopeFactory.CreateScope();

                var processor =
                    scope.ServiceProvider
                        .GetRequiredService<
                            IIncidentAIJobProcessor>();

                await processor.ProcessAsync(
                    job,
                    stoppingToken);

                _workerHealthState?.MarkSuccess("IncidentAIWorker");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "AI processing failed for incident {IncidentId}.",
                    job.IncidentId);
            }
        }

        _logger.LogInformation(
            "Incident AI background worker stopped.");
    }
}