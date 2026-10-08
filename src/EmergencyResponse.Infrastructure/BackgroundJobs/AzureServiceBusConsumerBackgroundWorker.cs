using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;
using EmergencyResponse.Application.ExternalContext;
using EmergencyResponse.Application.Reliability;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.Infrastructure.BackgroundJobs;

/// <summary>Consumes identifier-only durable jobs. Completion is acknowledged only after persisted effects succeed.</summary>
public sealed class AzureServiceBusConsumerBackgroundWorker(
    ServiceBusClient client, IServiceScopeFactory scopes, ILogger<AzureServiceBusConsumerBackgroundWorker> logger)
    : BackgroundService
{
    private readonly List<ServiceBusProcessor> _processors = [];
    private static readonly string[] Queues = ["incident-ai", "visual-analysis", "duplicate-detection", "external-context"];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var queue in Queues)
        {
            var processor = client.CreateProcessor(queue, new ServiceBusProcessorOptions
            {
                AutoCompleteMessages = false,
                MaxConcurrentCalls = 4,
                MaxAutoLockRenewalDuration = TimeSpan.FromMinutes(10)
            });
            processor.ProcessMessageAsync += OnMessage;
            processor.ProcessErrorAsync += OnError;
            _processors.Add(processor);
            await processor.StartProcessingAsync(stoppingToken);
        }
        try { await Task.Delay(Timeout.Infinite, stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }

    private async Task OnMessage(ProcessMessageEventArgs args)
    {
        var type = args.Message.Subject;
        var jobId = Guid.Empty;
        try
        {
            using var scope = scopes.CreateScope();
            var services = scope.ServiceProvider;
            var db = services.GetRequiredService<ApplicationDbContext>();
            var json = args.Message.Body.ToString();
            jobId = await DispatchAsync(type, json, services, db, args.CancellationToken);
            await args.CompleteMessageAsync(args.Message, args.CancellationToken);
        }
        catch (OperationCanceledException) when (args.CancellationToken.IsCancellationRequested)
        {
            await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
        catch (Exception ex)
        {
            var category = ReliabilityFailureClassifier.Categorize(ex);
            logger.LogWarning("Durable job {JobId} failed with category {Category}; delivery {DeliveryCount}.",
                jobId, category, args.Message.DeliveryCount);
            if (category is "Validation" or "Authentication" or "Permanent" or "ContentRejected" || args.Message.DeliveryCount >= 8)
                await args.DeadLetterMessageAsync(args.Message, "ProcessingFailed", category, args.CancellationToken);
            else
                await args.AbandonMessageAsync(args.Message, cancellationToken: args.CancellationToken);
        }
    }

    private static async Task<Guid> DispatchAsync(string type, string json, IServiceProvider services,
        ApplicationDbContext db, CancellationToken cancellationToken)
    {
        Guid jobId;
        Guid workItemId;
        switch (type)
        {
            case ReliabilityMessageTypes.IncidentAIAnalysisRequested:
                var ai = JsonSerializer.Deserialize<IncidentAIAnalysisRequested>(json)!;
                jobId = ai.JobId; workItemId = ai.IncidentId;
                await ExecuteIdempotently(db, jobId, type, ai.SchemaVersion, workItemId, async () =>
                    await services.GetRequiredService<IIncidentAIJobProcessor>().ProcessAsync(new IncidentAIJob(ai.IncidentId), cancellationToken), cancellationToken);
                break;
            case ReliabilityMessageTypes.VisualAnalysisRequested:
                var visual = JsonSerializer.Deserialize<VisualAnalysisRequested>(json)!;
                jobId = visual.JobId; workItemId = visual.MediaId;
                await ExecuteIdempotently(db, jobId, type, visual.SchemaVersion, workItemId, async () =>
                {
                    var media = await db.IncidentMedia.AsNoTracking().SingleAsync(x => x.Id == visual.MediaId, cancellationToken);
                    await services.GetRequiredService<EmergencyResponse.Infrastructure.AI.VisualAnalysisOrchestrationService>()
                        .AnalyzeAndPersistAsync(visual.MediaId, media.MediaType == IncidentMediaType.Video, cancellationToken);
                }, cancellationToken);
                break;
            case ReliabilityMessageTypes.DuplicateDetectionRequested:
                var duplicate = JsonSerializer.Deserialize<DuplicateDetectionRequested>(json)!;
                jobId = duplicate.JobId; workItemId = duplicate.IncidentId;
                await ExecuteIdempotently(db, jobId, type, duplicate.SchemaVersion, workItemId, async () =>
                {
                    var incident = await db.Incidents.SingleOrDefaultAsync(x => x.Id == duplicate.IncidentId, cancellationToken);
                    if (incident is null || incident.DuplicateDetectionStatus == DuplicateDetectionStatus.Completed) return;
                    var detector = services.GetRequiredService<IDuplicateIncidentService>();
                    incident.DuplicateDetectionStatus = DuplicateDetectionStatus.Processing;
                    await db.SaveChangesAsync(cancellationToken);
                    await detector.GenerateEmbeddingAsync(duplicate.IncidentId, cancellationToken);
                    await detector.FindCandidatesAsync(duplicate.IncidentId, cancellationToken: cancellationToken);
                    incident.DuplicateDetectionStatus = DuplicateDetectionStatus.Completed;
                    incident.DuplicateDetectionCompletedAtUtc = DateTime.UtcNow;
                    await db.SaveChangesAsync(cancellationToken);
                }, cancellationToken);
                break;
            case ReliabilityMessageTypes.ExternalContextEnrichmentRequested:
                var external = JsonSerializer.Deserialize<ExternalContextEnrichmentRequested>(json)!;
                jobId = external.JobId; workItemId = external.IncidentId;
                await ExecuteIdempotently(db, jobId, type, external.SchemaVersion, workItemId, async () =>
                {
                    var location = await db.IncidentLocations.AsNoTracking().FirstOrDefaultAsync(x => x.IncidentId == external.IncidentId && x.IsPrimary, cancellationToken);
                    if (location is not null)
                        await services.GetRequiredService<IIncidentExternalContextService>().CaptureAsync(external.IncidentId,
                            (double)location.Latitude, (double)location.Longitude, cancellationToken);
                }, cancellationToken);
                break;
            default: throw new InvalidDataException("Unknown durable job type.");
        }
        return jobId;
    }

    private static async Task ExecuteIdempotently(ApplicationDbContext db, Guid jobId, string type,
        string version, Guid workItemId, Func<Task> execute, CancellationToken cancellationToken)
    {
        var record = await db.BackgroundJobExecutions.SingleOrDefaultAsync(
            x => x.JobId == jobId && x.AnalysisVersion == version, cancellationToken);
        if (record?.Status == BackgroundJobStatus.Completed) return;
        if (record is null)
        {
            record = new BackgroundJobExecution { Id = Guid.NewGuid(), JobId = jobId, JobType = type,
                AnalysisVersion = version, WorkItemId = workItemId, Status = BackgroundJobStatus.Processing };
            db.BackgroundJobExecutions.Add(record);
        }
        record.Status = BackgroundJobStatus.Processing;
        record.AttemptCount++;
        record.LeaseExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10);
        await db.SaveChangesAsync(cancellationToken);
        try
        {
            await execute();
            record.Status = BackgroundJobStatus.Completed;
            record.CompletedAtUtc = DateTimeOffset.UtcNow;
            record.LeaseExpiresAtUtc = null;
            record.LastFailureCategory = null;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            record.Status = BackgroundJobStatus.Failed;
            record.LastFailureCategory = ReliabilityFailureClassifier.Categorize(ex);
            record.LeaseExpiresAtUtc = null;
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    private Task OnError(ProcessErrorEventArgs args)
    {
        logger.LogError("Service Bus processor error in {EntityPath}; source {ErrorSource}.", args.EntityPath, args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var processor in _processors)
        {
            await processor.StopProcessingAsync(cancellationToken);
            await processor.DisposeAsync();
        }
        await base.StopAsync(cancellationToken);
    }
}
