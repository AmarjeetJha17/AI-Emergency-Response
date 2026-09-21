using EmergencyResponse.Application.Map;

namespace EmergencyResponse.Web.Services;

public interface IEmergencyMapApiClient
{
    Task<IReadOnlyList<EmergencyMapIncident>> GetIncidentsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetIncidentTypesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDepartmentsAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MapResource>> GetResourcesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MapOperationalZone>> GetOperationalZonesAsync(
        CancellationToken cancellationToken = default);
}
