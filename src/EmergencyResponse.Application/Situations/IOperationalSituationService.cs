using EmergencyResponse.Application.Situations.DTOs;

namespace EmergencyResponse.Application.Situations;

public interface IOperationalSituationService
{
    Task EvaluateAsync(CancellationToken cancellationToken = default);

    Task<OperationalSituationResponse?> GetByIdAsync(
        Guid situationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalSituationResponse>> GetOpenAsync(
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

    Task<SituationMapItem> GetMapSituationAsync(
        Guid situationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SituationMapItem>> GetOpenForMapAsync(
        CancellationToken cancellationToken = default);

    Task ReanalyzeAsync(
        Guid situationId,
        CancellationToken cancellationToken = default);
}
