using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.DTOs.Common;
using EmergencyResponse.Application.DTOs.Incidents;

namespace EmergencyResponse.Web.Services;

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
}