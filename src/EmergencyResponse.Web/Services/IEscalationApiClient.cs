using EmergencyResponse.Application.Escalation.DTOs;

namespace EmergencyResponse.Web.Services;

public interface IEscalationApiClient
{
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
