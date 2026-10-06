using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Application.Situations;

namespace EmergencyResponse.Infrastructure.Copilot.Tools;

public sealed class SituationCopilotTools
{
    private readonly IOperationalSituationService _service;
    private readonly ICopilotExecutionContext _context;
    private readonly CopilotToolAuthorization _authorization;

    public SituationCopilotTools(
        IOperationalSituationService service,
        ICopilotExecutionContext context,
        CopilotToolAuthorization authorization)
    {
        _service = service;
        _context = context;
        _authorization = authorization;
    }

    public async Task<CopilotToolResult<CopilotSituationResult>> GetSituationAsync(
        Guid situationId,
        CancellationToken cancellationToken = default)
    {
        CopilotToolValidation.RequiredId(situationId, nameof(situationId));
        _authorization.RequireAuthenticated();

        var situation = await _service.GetByIdAsync(
            situationId,
            cancellationToken);
        if (situation is null)
        {
            return CopilotToolResult<CopilotSituationResult>.Unavailable(
                "NOT_FOUND",
                "The authorized situation could not be found.");
        }

        return CopilotToolResult<CopilotSituationResult>.Available(
            new CopilotSituationResult
            {
                SituationId = situation.Id,
                SituationNumber = situation.SituationNumber,
                Title = situation.Title,
                Summary = situation.Summary ?? string.Empty,
                Level = situation.LevelName,
                Status = situation.StatusName,
                SituationScore = situation.SituationScore,
                IncidentCount = situation.IncidentCount,
                CriticalIncidentCount = situation.CriticalIncidentCount,
                HighIncidentCount = situation.HighIncidentCount,
                PeopleAffected = situation.PeopleAffected,
                Casualties = situation.Casualties,
                ResourceShortageDetected = situation.ResourceShortageDetected,
                RetrievedAtUtc = situation.LastEvaluatedAtUtc ?? DateTime.UtcNow,
                Incidents = situation.Incidents
                    .Take(50)
                    .Select(x => new CopilotSituationIncident
                    {
                        IncidentId = x.IncidentId,
                        IncidentNumber = x.IncidentNumber,
                        Title = x.Title,
                        Severity = x.Severity.ToString(),
                        Priority = x.Priority.ToString(),
                        IsPrimaryIncident = x.IsPrimaryIncident
                    })
                    .ToList()
            });
    }

    public Task<CopilotToolResult<CopilotSituationResult>> GetCurrentSituationAsync(
        CancellationToken cancellationToken = default) =>
        _context.SituationId is Guid situationId
            ? GetSituationAsync(situationId, cancellationToken)
            : Task.FromResult(
                CopilotToolResult<CopilotSituationResult>.Unavailable(
                    "NO_CONTEXT",
                    "No trusted current situation context is available."));
}
