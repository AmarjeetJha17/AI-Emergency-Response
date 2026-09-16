using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;

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

    public IncidentAIBackgroundWorker(
        IAIAnalysisQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<IncidentAIBackgroundWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Incident AI background worker started.");

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