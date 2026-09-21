namespace EmergencyResponse.Application.Map;

public interface IEmergencyMapService
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

    IReadOnlyList<MapOperationalZone> BuildOperationalZones(
        IReadOnlyList<EmergencyMapIncident> incidents);
}
