namespace EmergencyResponse.Application.Media;

public interface IIncidentMediaService
{
    Task<Guid> SaveImageAsync(
        Guid incidentId,
        Stream content,
        string fileName,
        string contentType,
        long length,
        CancellationToken cancellationToken = default);

    Task<Guid> SaveVideoAsync(
        Guid incidentId,
        Stream content,
        string fileName,
        string contentType,
        long length,
        CancellationToken cancellationToken = default);
}
