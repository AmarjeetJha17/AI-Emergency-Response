using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;

namespace EmergencyResponse.Infrastructure.Persistence.Repositories;

public sealed class AIAnalysisRepository
    : IAIAnalysisRepository
{
    private readonly ApplicationDbContext _dbContext;

    public AIAnalysisRepository(
        ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(
        AIAnalysis analysis,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.AIAnalyses.AddAsync(
            analysis,
            cancellationToken);

        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}