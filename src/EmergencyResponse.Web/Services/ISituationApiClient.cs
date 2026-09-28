using EmergencyResponse.Application.Situations.DTOs;

namespace EmergencyResponse.Web.Services;

public interface ISituationApiClient
{
    Task<IReadOnlyList<OperationalSituationResponse>> GetOpenSituationsAsync(
        CancellationToken cancellationToken = default);

    Task<OperationalSituationResponse?> GetByIdAsync(
        Guid situationId,
        CancellationToken cancellationToken = default);

    Task EvaluateAsync(
        CancellationToken cancellationToken = default);

    Task ReviewAsync(
        Guid situationId,
        ReviewSituationRequest request,
        CancellationToken cancellationToken = default);

    Task AcknowledgeAsync(
        Guid situationId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task ResolveAsync(
        Guid situationId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task DismissAsync(
        Guid situationId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SituationCommandCenterItem>> GetCommandCenterAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SituationMapItem>> GetOpenForMapAsync(
        CancellationToken cancellationToken = default);
}
