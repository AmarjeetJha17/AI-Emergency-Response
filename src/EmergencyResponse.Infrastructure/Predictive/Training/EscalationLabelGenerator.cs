using EmergencyResponse.Application.Predictive.Training;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Predictive.Training;

public sealed class EscalationLabelGenerator : IEscalationLabelGenerator
{
    private readonly ApplicationDbContext _dbContext;

    public EscalationLabelGenerator(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EscalationLabelResult> GenerateAsync(
        Guid incidentId,
        DateTime snapshotAtUtc,
        CancellationToken cancellationToken = default)
    {
        var windowEnd = snapshotAtUtc.AddHours(6);
        var escalation = await _dbContext.IncidentEscalations
            .AsNoTracking()
            .Where(x =>
                x.IncidentId == incidentId &&
                x.CreatedAtUtc > snapshotAtUtc &&
                x.CreatedAtUtc <= windowEnd)
            .OrderBy(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new EscalationLabelResult
        {
            Label = escalation is not null,
            OutcomeAtUtc = escalation?.CreatedAtUtc,
            OutcomeEscalationId = escalation?.Id
        };
    }
}
