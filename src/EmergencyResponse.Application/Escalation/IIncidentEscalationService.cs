using EmergencyResponse.Application.Escalation.DTOs;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Escalation;

public interface IIncidentEscalationService
{
    Task<IReadOnlyList<IncidentEscalationResponse>> EvaluateIncidentAsync(
        Guid incidentId,
        SeverityLevel? previousSeverity = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncidentEscalationResponse>> GetOpenEscalationsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncidentEscalationResponse>> GetByIncidentIdAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task AcknowledgeAsync(
        Guid escalationId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task ResolveAsync(
        Guid escalationId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task DismissAsync(
        Guid escalationId,
        string? notes,
        CancellationToken cancellationToken = default);
}
