using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Copilot.Tools;

public sealed class EscalationCopilotTools
{
    private readonly ApplicationDbContext _db;
    private readonly CopilotToolAuthorization _authorization;

    public EscalationCopilotTools(
        ApplicationDbContext db,
        CopilotToolAuthorization authorization)
    {
        _db = db;
        _authorization = authorization;
    }

    public async Task<CopilotToolResult<IReadOnlyList<CopilotEscalationResult>>>
        GetOpenEscalationsAsync(
            Guid? incidentId = null,
            Guid? situationId = null,
            CancellationToken cancellationToken = default)
    {
        if (incidentId is Guid id)
        {
            CopilotToolValidation.RequiredId(id, nameof(incidentId));
            await _authorization.RequireIncidentAccessAsync(id, cancellationToken);
        }
        else
        {
            _authorization.RequireAuthenticated();
        }

        var query = _db.IncidentEscalations
            .AsNoTracking()
            .Include(x => x.Incident)
            .Where(x => x.Status == EscalationStatus.Open);
        if (incidentId is Guid requestedIncidentId)
        {
            query = query.Where(x => x.IncidentId == requestedIncidentId);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .Select(x => new CopilotEscalationResult
            {
                Id = x.Id,
                IncidentId = x.IncidentId,
                IncidentNumber = x.Incident.IncidentNumber,
                Type = x.Type.ToString(),
                Level = x.Level.ToString(),
                Status = x.Status.ToString(),
                Reason = x.Reason,
                Score = x.Score ?? 0,
                RequiresHumanReview = x.RequiresHumanReview,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return CopilotToolResult<IReadOnlyList<CopilotEscalationResult>>.Available(items);
    }
}
