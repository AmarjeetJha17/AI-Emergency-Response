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

    Task<PagedResult<IncidentListItem>> GetPagedForCurrentUserAsync(
        int page,
        int pageSize,
        string? search,
        Guid? incidentTypeId,
        string? status,
        string? severity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions an incident to a new status, validates the transition is
    /// permitted, and appends a <see cref="Domain.Entities.IncidentStatusHistory"/> record.
    /// Returns the updated incident, or null if not found.
    /// Throws <see cref="InvalidOperationException"/> for illegal transitions.
    /// </summary>
    Task<IncidentResponse?> UpdateStatusAsync(
        Guid id,
        UpdateIncidentStatusRequest request,
        CancellationToken cancellationToken = default);
}