using EmergencyResponse.Application.DTOs.IncidentTypes;

namespace EmergencyResponse.Web.Services;

public interface IIncidentTypeApiClient
{
    Task<IReadOnlyCollection<IncidentTypeResponse>>
        GetAllAsync(
            CancellationToken cancellationToken = default);
}