using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Application.Interfaces;

namespace EmergencyResponse.Infrastructure.Copilot.Tools;

public sealed class IncidentCopilotTools
{
    private readonly IIncidentService _incidentService;
    private readonly ICopilotExecutionContext _context;
    private readonly CopilotToolAuthorization _authorization;

    public IncidentCopilotTools(
        IIncidentService incidentService,
        ICopilotExecutionContext context,
        CopilotToolAuthorization authorization)
    {
        _incidentService = incidentService;
        _context = context;
        _authorization = authorization;
    }

    public async Task<CopilotToolResult<CopilotIncidentResult>?> GetIncidentAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        CopilotToolValidation.RequiredId(incidentId, nameof(incidentId));
        await _authorization.RequireIncidentAccessAsync(
            incidentId,
            cancellationToken);

        var incident = await _incidentService.GetByIdAsync(
            incidentId,
            cancellationToken);
        return incident is null
            ? CopilotToolResult<CopilotIncidentResult>.Unavailable(
                "NOT_FOUND",
                "The authorized incident could not be found.")
            : CopilotToolResult<CopilotIncidentResult>.Available(
                new CopilotIncidentResult
                {
                    IncidentId = incident.Id,
                    IncidentNumber = incident.IncidentNumber,
                    Title = incident.Title,
                    Description = incident.Description,
                    IncidentType = incident.IncidentType,
                    Status = incident.Status,
                    Severity = incident.Severity,
                    Priority = incident.Priority,
                    PeopleAffected = incident.PeopleAffected,
                    VehiclesAffected = incident.VehiclesAffected,
                    Casualties = incident.Casualties,
                    LastUpdatedAtUtc = incident.ReportedAtUtc
                });
    }

    public Task<CopilotToolResult<CopilotIncidentResult>?> GetCurrentIncidentAsync(
        CancellationToken cancellationToken = default) =>
        _context.IncidentId is Guid incidentId
            ? GetIncidentAsync(incidentId, cancellationToken)
            : Task.FromResult<CopilotToolResult<CopilotIncidentResult>?>(null);
}
