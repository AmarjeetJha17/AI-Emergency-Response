using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Security.SecurityAudit;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.BackgroundJobs;

public sealed class DataRetentionBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<RetentionOptions> _options;
    private readonly ILogger<DataRetentionBackgroundWorker> _logger;

    public DataRetentionBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RetentionOptions> options,
        ILogger<DataRetentionBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var conversationCutoff = now.AddDays(-_options.Value.CopilotConversationDays);
        var notificationCutoff = now.AddDays(-_options.Value.NotificationDays);
        var archivedConversations = 0;
        var deletedNotifications = 0;

        await using (var scope = _scopeFactory.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            archivedConversations = await db.CopilotConversations
                .Where(x => x.ArchivedAtUtc == null && x.LastActivityAtUtc < conversationCutoff)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.ArchivedAtUtc, now)
                        .SetProperty(x => x.UpdatedAtUtc, now),
                    cancellationToken);

            deletedNotifications = await db.Notifications
                .Where(x => x.CreatedAtUtc < notificationCutoff ||
                            (x.ExpiresAtUtc != null && x.ExpiresAtUtc < now))
                .ExecuteDeleteAsync(cancellationToken);

            if (archivedConversations > 0 || deletedNotifications > 0)
            {
                db.AuditLogs.Add(new AuditLog
                {
                    Id = Guid.NewGuid(),
                    Action = SecurityEventTypes.DataRetentionExecuted,
                    EntityName = "DataRetention",
                    EntityId = "batch",
                    OldValues = null,
                    NewValues = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        archivedConversations,
                        deletedNotifications
                    }),
                    OccurredAtUtc = now
                });
                await db.SaveChangesAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }

        if (archivedConversations > 0 || deletedNotifications > 0)
        {
            _logger.LogInformation(
                "Data retention cleanup completed. ArchivedConversations={ArchivedConversations} DeletedNotifications={DeletedNotifications}",
                archivedConversations,
                deletedNotifications);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(Math.Clamp(_options.Value.CleanupIntervalHours, 1, 168));
        using var timer = new PeriodicTimer(interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Data retention cleanup failed.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
