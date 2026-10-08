using System.Text.Json;
using EmergencyResponse.Application.Observability;
using EmergencyResponse.Application.Reliability;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.BackgroundJobs;

public sealed class OutboxPublisherBackgroundWorker(IServiceScopeFactory scopeFactory,
    IApplicationMessageBus bus, ILogger<OutboxPublisherBackgroundWorker> logger,
    IOptions<ReliableMessagingOptions> retryOptions)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var pending = await db.OutboxMessages
                    .Where(x => !x.IsPublished)
                    .OrderBy(x => x.OccurredAtUtc)
                    .Take(50)
                    .ToListAsync(stoppingToken);
                foreach (var message in pending)
                {
                    message.AttemptCount++;
                    message.LastAttemptAtUtc = DateTimeOffset.UtcNow;
                    try
                    {
                        await bus.PublishAsync<JsonElement>(message.MessageType,
                            JsonSerializer.Deserialize<JsonElement>(message.PayloadJson),
                            message.MessageKey, stoppingToken);
                        message.IsPublished = true;
                        message.PublishedAtUtc = DateTimeOffset.UtcNow;
                        message.LastError = null;
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        message.LastError = ReliabilityFailureClassifier.Categorize(ex);
                        logger.LogWarning("Outbox publish failed for {MessageKey}; category {Category}; attempt {Attempt}.",
                            message.MessageKey, message.LastError, message.AttemptCount);
                    }
                    await db.SaveChangesAsync(stoppingToken);
                }
                var delay = pending.Count == 0 ? TimeSpan.FromSeconds(2) : TimeSpan.FromMilliseconds(100);
                var failedAttempt = pending.Where(x => !x.IsPublished).Select(x => x.AttemptCount).DefaultIfEmpty(0).Max();
                if (failedAttempt > 0)
                {
                    var steps = retryOptions.Value.BackoffSeconds ?? [2, 5, 15, 60];
                    var seconds = steps[Math.Min(failedAttempt - 1, steps.Length - 1)];
                    var jitter = Random.Shared.Next(0, Math.Max(1, retryOptions.Value.JitterMilliseconds));
                    delay = TimeSpan.FromSeconds(seconds) + TimeSpan.FromMilliseconds(jitter);
                }
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError("Outbox publisher iteration failed ({Category}).", ReliabilityFailureClassifier.Categorize(ex));
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
