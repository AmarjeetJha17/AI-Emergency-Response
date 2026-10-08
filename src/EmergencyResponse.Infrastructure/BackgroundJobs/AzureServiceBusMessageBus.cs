using System.Text.Json;
using Azure.Messaging.ServiceBus;
using EmergencyResponse.Application.Reliability;

namespace EmergencyResponse.Infrastructure.BackgroundJobs;

public sealed class AzureServiceBusMessageBus(ServiceBusClient client) : IApplicationMessageBus
{
    public async Task PublishAsync<T>(string messageType, T message, string messageId,
        CancellationToken cancellationToken = default)
    {
        var queue = messageType switch
        {
            ReliabilityMessageTypes.IncidentAIAnalysisRequested => "incident-ai",
            ReliabilityMessageTypes.VisualAnalysisRequested => "visual-analysis",
            ReliabilityMessageTypes.DuplicateDetectionRequested => "duplicate-detection",
            ReliabilityMessageTypes.ExternalContextEnrichmentRequested => "external-context",
            _ => throw new InvalidOperationException($"Unsupported message type '{messageType}'.")
        };
        var sender = client.CreateSender(queue);
        try
        {
            await sender.SendMessageAsync(new ServiceBusMessage(BinaryData.FromString(
                JsonSerializer.Serialize(message)))
            {
                MessageId = messageId,
                Subject = messageType,
                ContentType = "application/json"
            }, cancellationToken);
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}
