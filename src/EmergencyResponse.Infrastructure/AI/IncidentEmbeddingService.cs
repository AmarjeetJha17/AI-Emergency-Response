using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.Interfaces;

using Microsoft.Extensions.AI;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class IncidentEmbeddingService
    : IIncidentEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>>
        _embeddingGenerator;

    public IncidentEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _embeddingGenerator = embeddingGenerator;
    }

    public async Task<ReadOnlyMemory<float>> GenerateAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);

        var embeddings = await _embeddingGenerator.GenerateAsync(
            [text],
            cancellationToken: cancellationToken);

        if (embeddings.Count == 0)
        {
            throw new InvalidOperationException(
                "The embedding service returned no embedding.");
        }

        return embeddings[0].Vector;
    }
}