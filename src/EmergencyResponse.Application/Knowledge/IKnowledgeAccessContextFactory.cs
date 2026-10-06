namespace EmergencyResponse.Application.Knowledge;

public interface IKnowledgeAccessContextFactory
{
    Task<KnowledgeAccessContext> CreateAsync(
        CancellationToken cancellationToken = default);
}
