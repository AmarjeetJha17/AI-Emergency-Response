namespace EmergencyResponse.Application.Map;

public interface IEmergencyMapService
{
    Task<IReadOnlyList<EmergencyMapIncident>> GetIncidentsAsync(
        CancellationToken cancellationToken = default);
}
