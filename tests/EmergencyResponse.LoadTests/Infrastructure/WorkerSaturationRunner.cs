using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.LoadTests.Infrastructure;

public static class WorkerSaturationRunner
{
    public static async Task<object> RunAsync(LoadTestHostFactory host, LoadTestOptions options, CancellationToken cancellationToken)
    {
        var started = DateTime.UtcNow;
        host.Faults.AIDelayMs = options.WorkerAIDelayMs;
        var enqueueTimer = System.Diagnostics.Stopwatch.StartNew();
        await using (var scope = host.Services.CreateAsyncScope())
        {
            var queue = scope.ServiceProvider.GetRequiredService<IAIAnalysisQueue>();
            for (var index = 0; index < options.QueueJobCount; index++)
            {
                await queue.EnqueueAsync(new IncidentAIJob(host.Data.IncidentIds[index % host.Data.IncidentIds.Count]), cancellationToken);
            }
        }
        enqueueTimer.Stop();

        var processed = 0;
        var failed = 0;
        var queueAgeSeconds = 0d;
        var startedAt = new DateTime(started.Ticks, DateTimeKind.Utc);
        var timeout = options.QueueJobCount >= 5_000 ? TimeSpan.FromMinutes(30) : TimeSpan.FromMinutes(10);
        var drainTimer = System.Diagnostics.Stopwatch.StartNew();
        while (drainTimer.Elapsed < timeout)
        {
            await using var scope = host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var statuses = await db.AIAnalyses.AsNoTracking()
                .Where(item => item.LastAttemptAtUtc >= startedAt)
                .GroupBy(item => item.Status)
                .Select(group => new { Status = group.Key, Count = group.Count() })
                .ToListAsync(cancellationToken);
            processed = statuses.Where(item => item.Status is AIAnalysisStatus.Completed or AIAnalysisStatus.RequiresReview or AIAnalysisStatus.Failed).Sum(item => item.Count);
            failed = statuses.Where(item => item.Status == AIAnalysisStatus.Failed).Sum(item => item.Count);
            if (processed >= options.QueueJobCount) break;
            if (processed > 0 && queueAgeSeconds == 0) queueAgeSeconds = drainTimer.Elapsed.TotalSeconds;
            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
        drainTimer.Stop();
        host.Faults.AIDelayMs = 0;

        var durationSeconds = Math.Max(0.001, drainTimer.Elapsed.TotalSeconds);
        return new
        {
            scenario = "worker-saturation",
            environment = "LocalTestHost with one configured in-process incident AI worker",
            jobsEnqueued = options.QueueJobCount,
            jobsProcessed = processed,
            failedJobs = failed,
            backlogAtEnd = Math.Max(0, options.QueueJobCount - processed),
            fakeProviderDelayMs = options.WorkerAIDelayMs,
            enqueueSeconds = Math.Round(enqueueTimer.Elapsed.TotalSeconds, 2),
            processingSeconds = Math.Round(durationSeconds, 2),
            throughputJobsPerMinute = Math.Round(processed / durationSeconds * 60, 2),
            firstCompletedJobAgeSeconds = Math.Round(queueAgeSeconds, 2),
            recovered = processed >= options.QueueJobCount && failed == 0,
            note = "This measures the current single local worker and SQL writes. It does not model Azure Service Bus or configurable worker-count scaling."
        };
    }
}
