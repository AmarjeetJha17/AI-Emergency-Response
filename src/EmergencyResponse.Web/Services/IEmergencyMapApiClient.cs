using EmergencyResponse.Application.Map;

namespace EmergencyResponse.Web.Services;

public interface IEmergencyMapApiClient
{
    Task<IReadOnlyList<EmergencyMapIncident>> GetIncidentsAsync(
        CancellationToken cancellationToken = default);
}
