using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.DTOs.Common;
using EmergencyResponse.Application.DTOs.Incidents;

namespace EmergencyResponse.Web.Services;

public sealed record MediaStreamResult(
    Stream Stream,
    string ContentType);

public interface IIncidentApiClient
{
    Task<IncidentResponse?> CreateAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default);

    Task<IncidentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DuplicateIncidentCandidate>>
        GetDuplicatesAsync(
            Guid incidentId,
            CancellationToken cancellationToken = default);

    Task ReviewDuplicateAsync(
        Guid incidentId,
        Guid matchId,
        ReviewDuplicateIncidentRequest request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<IncidentListItem>?> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        Guid? incidentTypeId = null,
        string? status = null,
        string? severity = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IncidentMediaResponse>> GetMediaAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);

    Task<VisualAnalysisResponse?> GetVisualAnalysisAsync(
        Guid incidentId,
        Guid mediaId,
        CancellationToken cancellationToken = default);

    Task<MediaStreamResult?> GetMediaStreamAsync(
        Guid incidentId,
        Guid mediaId,
        CancellationToken cancellationToken = default);

    Task ReviewVisualAnalysisAsync(
        Guid incidentId,
        Guid mediaId,
        ReviewVisualAnalysisRequest request,
        CancellationToken cancellationToken = default);

    Task<MediaStreamResult?> GetFrameAsync(
        Guid incidentId,
        Guid mediaId,
        int frameNumber,
        CancellationToken cancellationToken = default);
}