namespace EmergencyResponse.Application.AI.Interfaces;

public interface IIncidentEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default);
}