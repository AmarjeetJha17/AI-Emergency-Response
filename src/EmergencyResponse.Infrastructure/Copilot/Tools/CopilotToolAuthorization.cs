using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Copilot.Tools;

public sealed class CopilotToolAuthorization
{
    private readonly ApplicationDbContext _db;
    private readonly ICopilotExecutionContext _context;

    public CopilotToolAuthorization(
        ApplicationDbContext db,
        ICopilotExecutionContext context)
    {
        _db = db;
        _context = context;
    }

    public void RequireAuthenticated()
    {
        if (!_context.IsAuthenticated)
        {
            throw new CopilotToolAuthorizationException();
        }
    }

    public async Task RequireIncidentAccessAsync(
        Guid incidentId,
        CancellationToken cancellationToken)
    {
        RequireAuthenticated();

        if (HasGlobalAccess())
        {
            return;
        }

        var allowed = await _db.Incidents
            .AsNoTracking()
            .Where(x => x.Id == incidentId)
            .Select(x => x.ReporterUserId == _context.UserId ||
                         (x.DepartmentId.HasValue &&
                          x.DepartmentId == _context.DepartmentId))
            .SingleOrDefaultAsync(cancellationToken);

        if (!allowed)
        {
            throw new CopilotToolAuthorizationException();
        }
    }

    public bool HasGlobalAccess() =>
        _context.Roles.Any(role =>
            role is ApplicationRoles.EmergencyAdmin
                or ApplicationRoles.SystemAdmin
                or ApplicationRoles.Operator);
}
