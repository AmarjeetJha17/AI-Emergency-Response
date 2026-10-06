namespace EmergencyResponse.Application.Knowledge;

public interface IKnowledgeDocumentNumberGenerator
{
    Task<string> GenerateAsync(
        CancellationToken cancellationToken = default);
}
