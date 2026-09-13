using EmergencyResponse.Application.DTOs.Common;
using EmergencyResponse.Application.DTOs.Incidents;

namespace EmergencyResponse.Application.Interfaces;

public interface IIncidentService
{
    Task<IncidentResponse> CreateAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default);

    Task<IncidentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<IncidentListItem>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        Guid? incidentTypeId,
        string? status,
        string? severity,
        CancellationToken cancellationToken = default);
}