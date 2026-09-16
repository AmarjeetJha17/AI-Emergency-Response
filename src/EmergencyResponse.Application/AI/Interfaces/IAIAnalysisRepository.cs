using EmergencyResponse.Domain.Entities;

namespace EmergencyResponse.Application.AI.Interfaces;

public interface IAIAnalysisRepository
{
    Task AddAsync(
        AIAnalysis analysis,
        CancellationToken cancellationToken = default);
}