using System.Text.Json;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.Jobs;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.ExternalContext;
using EmergencyResponse.Application.Reliability;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.BackgroundJobs;

/// <summary>Local adapter; messages are handed to the existing in-process workers.</summary>
public sealed class InMemoryApplicationMessageBus(
    IServiceScopeFactory scopes,
    IOptions<AIOptions> aiOptions) : IApplicationMessageBus
{
    public async Task PublishAsync<T>(string messageType, T message, string messageId,
        CancellationToken cancellationToken = default)
    {
        if (!aiOptions.Value.Enabled && messageType is
            ReliabilityMessageTypes.IncidentAIAnalysisRequested or
            ReliabilityMessageTypes.VisualAnalysisRequested or
            ReliabilityMessageTypes.DuplicateDetectionRequested)
        {
            return;
        }

        var json = JsonSerializer.Serialize(message);
        using var scope = scopes.CreateScope();
        var services = scope.ServiceProvider;
        switch (messageType)
        {
            case ReliabilityMessageTypes.IncidentAIAnalysisRequested:
            {
                var item = JsonSerializer.Deserialize<IncidentAIAnalysisRequested>(json)!;
                await services.GetRequiredService<IAIAnalysisQueue>().EnqueueAsync(new IncidentAIJob(item.IncidentId), cancellationToken);
                break;
            }
            case ReliabilityMessageTypes.VisualAnalysisRequested:
            {
                var item = JsonSerializer.Deserialize<VisualAnalysisRequested>(json)!;
                await services.GetRequiredService<IVisualAnalysisQueue>().EnqueueAsync(new VisualAnalysisJob { IncidentMediaId = item.MediaId }, cancellationToken);
                break;
            }
            case ReliabilityMessageTypes.ExternalContextEnrichmentRequested:
            {
                var item = JsonSerializer.Deserialize<ExternalContextEnrichmentRequested>(json)!;
                var db = services.GetRequiredService<ApplicationDbContext>();
                var location = await db.IncidentLocations.AsNoTracking().FirstOrDefaultAsync(x => x.IncidentId == item.IncidentId && x.IsPrimary, cancellationToken);
                if (location is not null)
                    await services.GetRequiredService<IExternalContextQueue>().EnqueueAsync(new ExternalContextJob(item.IncidentId, (double)location.Latitude, (double)location.Longitude), cancellationToken);
                break;
            }
            case ReliabilityMessageTypes.DuplicateDetectionRequested:
                // Local AI processing already performs duplicate detection as part of its job.
                break;
            default: throw new InvalidOperationException($"Unsupported message type '{messageType}'.");
        }
    }
}
