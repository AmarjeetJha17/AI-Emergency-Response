using EmergencyResponse.Application.Knowledge;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.Knowledge;

public sealed class KnowledgeDocumentBackgroundWorker : BackgroundService
{
    private readonly IKnowledgeDocumentQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KnowledgeDocumentBackgroundWorker> _logger;

    public KnowledgeDocumentBackgroundWorker(
        IKnowledgeDocumentQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<KnowledgeDocumentBackgroundWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider
                    .GetRequiredService<IKnowledgeDocumentService>();
                await service.ProcessAsync(
                    job.DocumentId,
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Knowledge document background processing failed.");
            }
        }
    }
}
