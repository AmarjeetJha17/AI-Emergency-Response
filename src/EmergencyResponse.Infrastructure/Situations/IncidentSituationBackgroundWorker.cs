using EmergencyResponse.Application.Observability;
using EmergencyResponse.Application.Situations;
using EmergencyResponse.Application.Situations.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.Situations;

public sealed class IncidentSituationBackgroundWorker : BackgroundService
{
    private readonly ISituationEvaluationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<IncidentSituationBackgroundWorker> _logger;
    private readonly IWorkerHealthState? _workerHealthState;

    public IncidentSituationBackgroundWorker(
        ISituationEvaluationQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<IncidentSituationBackgroundWorker> logger,
        IWorkerHealthState? workerHealthState = null)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _workerHealthState = workerHealthState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Incident situation background worker started.");
        _workerHealthState?.MarkStarted("IncidentSituationWorker");

        while (!stoppingToken.IsCancellationRequested)
        {
            SituationEvaluationJob job;

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
                var service = scope.ServiceProvider.GetRequiredService<IOperationalSituationService>();

                await service.EvaluateAsync(stoppingToken);
                _workerHealthState?.MarkSuccess("IncidentSituationWorker");
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Situation evaluation failed for job triggered by incident {IncidentId} (Reason: {Reason}).",
                    job.TriggerIncidentId,
                    job.TriggerReason);
            }
        }

        _logger.LogInformation("Incident situation background worker stopped.");
    }
}
