using System.Text.Json;
using EmergencyResponse.Application.Reliability;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;

namespace EmergencyResponse.Infrastructure.BackgroundJobs;

/// <summary>Persists compact, identifier-only job requests in the caller's EF unit of work.</summary>
public sealed class OutboxBackgroundJobScheduler(ApplicationDbContext db) : IBackgroundJobScheduler
{
    private const string SchemaVersion = "1";

    public Guid ScheduleIncidentAI(Guid incidentId)
    {
        var jobId = Guid.NewGuid();
        return Add(ReliabilityMessageTypes.IncidentAIAnalysisRequested,
            new IncidentAIAnalysisRequested(incidentId, jobId, SchemaVersion), jobId);
    }

    public Guid ScheduleDuplicateDetection(Guid incidentId)
    {
        var jobId = Guid.NewGuid();
        return Add(ReliabilityMessageTypes.DuplicateDetectionRequested,
            new DuplicateDetectionRequested(incidentId, jobId, SchemaVersion), jobId);
    }

    public Guid ScheduleExternalContext(Guid incidentId)
    {
        var jobId = Guid.NewGuid();
        return Add(ReliabilityMessageTypes.ExternalContextEnrichmentRequested,
            new ExternalContextEnrichmentRequested(incidentId, jobId, SchemaVersion), jobId);
    }

    public Guid ScheduleVisualAnalysis(Guid incidentId, Guid mediaId)
    {
        var jobId = Guid.NewGuid();
        return Add(ReliabilityMessageTypes.VisualAnalysisRequested,
            new VisualAnalysisRequested(incidentId, mediaId, jobId, SchemaVersion), jobId);
    }

    private Guid Add<T>(string type, T payload, Guid jobId)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = type,
            MessageKey = jobId.ToString("N"),
            PayloadJson = JsonSerializer.Serialize(payload),
            OccurredAtUtc = DateTimeOffset.UtcNow
        });
        return jobId;
    }
}
