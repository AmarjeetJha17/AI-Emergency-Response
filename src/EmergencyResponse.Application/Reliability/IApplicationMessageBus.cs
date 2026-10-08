namespace EmergencyResponse.Application.Reliability;

public interface IApplicationMessageBus
{
    Task PublishAsync<T>(
        string messageType,
        T message,
        string messageId,
        CancellationToken cancellationToken = default);
}
