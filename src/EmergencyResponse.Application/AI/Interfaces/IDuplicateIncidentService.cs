using EmergencyResponse.Application.AI.DTOs;

namespace EmergencyResponse.Application.AI.Interfaces;

public interface IDuplicateIncidentService
{
    Task GenerateEmbeddingAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DuplicateIncidentCandidate>>
        FindCandidatesAsync(
            Guid incidentId,
            int maxCandidates = 20,
            CancellationToken cancellationToken = default);
}