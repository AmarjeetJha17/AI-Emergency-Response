using EmergencyResponse.Application.Reliability;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.LoadTests.Infrastructure;

public static class QueuePressureRunner
{
    public static async Task<object> RunAsync(
        LoadTestHostFactory host,
        LoadTestOptions options,
        CancellationToken cancellationToken)
    {
        var faults = host.Faults;
        faults.PublishUnavailable = true;
        var created = 0;
        try
        {
            await using var scope = host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var scheduler = scope.ServiceProvider.GetRequiredService<IBackgroundJobScheduler>();
            for (var index = 0; index < options.QueueJobCount; index++)
            {
                scheduler.ScheduleIncidentAI(host.Data.IncidentIds[index % host.Data.IncidentIds.Count]);
                created++;
                if (created % 500 == 0)
                {
                    await db.SaveChangesAsync(cancellationToken);
                    db.ChangeTracker.Clear();
                }
            }
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
        catch
        {
            faults.PublishUnavailable = false;
            throw;
        }

        var beforeRecovery = await ReadQueueAsync(host.Services, cancellationToken);
        faults.PublishUnavailable = false;
        var recoveryStarted = DateTime.UtcNow;
        var recoveryLimit = TimeSpan.FromMinutes(options.QueueJobCount >= 5_000 ? 15 : 5);
        var afterRecovery = beforeRecovery;
        while (DateTime.UtcNow - recoveryStarted < recoveryLimit)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            afterRecovery = await ReadQueueAsync(host.Services, cancellationToken);
            if (afterRecovery.Pending == 0)
            {
                break;
            }
        }

        return new
        {
            scenario = "durable-outbox-pressure-and-recovery",
            environment = "LocalTestHost with SQL Server and in-process fake workers",
            jobsCreated = created,
            pendingAtRecoveryStart = beforeRecovery.Pending,
            oldestPendingAgeSeconds = beforeRecovery.OldestAgeSeconds,
            pendingAfterRecovery = afterRecovery.Pending,
            publishedAfterRecovery = afterRecovery.Published,
            recoverySeconds = Math.Round((DateTime.UtcNow - recoveryStarted).TotalSeconds, 2),
            recovered = afterRecovery.Pending == 0,
            note = "Outbox backlog is measured from SQL. The in-process bounded worker queue does not currently expose per-job age or depth through the API."
        };
    }

    private static async Task<QueueSnapshot> ReadQueueAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var pending = await db.OutboxMessages.AsNoTracking().Where(message => !message.IsPublished)
            .Select(message => message.OccurredAtUtc).ToListAsync(cancellationToken);
        var published = await db.OutboxMessages.AsNoTracking().CountAsync(message => message.IsPublished, cancellationToken);
        var oldestAge = pending.Count == 0
            ? 0
            : Math.Max(0, (DateTimeOffset.UtcNow - pending.Min()).TotalSeconds);
        return new QueueSnapshot(pending.Count, published, Math.Round(oldestAge, 2));
    }

    private sealed record QueueSnapshot(int Pending, int Published, double OldestAgeSeconds);
}
