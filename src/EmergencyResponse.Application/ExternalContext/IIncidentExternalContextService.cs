using EmergencyResponse.Domain.Entities;

namespace EmergencyResponse.Application.ExternalContext;

public interface IIncidentExternalContextService
{
    Task<IncidentEnvironmentalContext?> CaptureAsync(
        Guid incidentId,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default);
}
