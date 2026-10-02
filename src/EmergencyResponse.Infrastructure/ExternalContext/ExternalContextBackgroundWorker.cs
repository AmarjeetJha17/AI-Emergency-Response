using EmergencyResponse.Application.ExternalContext;
using EmergencyResponse.Application.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.ExternalContext;

public sealed class ExternalContextBackgroundWorker : BackgroundService
{
    private readonly IExternalContextQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExternalContextBackgroundWorker> _logger;
    private readonly IWorkerHealthState? _workerHealthState;

    public ExternalContextBackgroundWorker(
        IExternalContextQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ExternalContextBackgroundWorker> logger,
        IWorkerHealthState? workerHealthState = null)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _workerHealthState = workerHealthState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _workerHealthState?.MarkStarted(nameof(ExternalContextBackgroundWorker));
        while (!stoppingToken.IsCancellationRequested)
        {
            ExternalContextJob job;
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
                var service = scope.ServiceProvider
                    .GetRequiredService<IIncidentExternalContextService>();
                await service.CaptureAsync(
                    job.IncidentId,
                    job.Latitude,
                    job.Longitude,
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "External context capture failed for incident {IncidentId}.",
                    job.IncidentId);
            }
        }
    }
}
