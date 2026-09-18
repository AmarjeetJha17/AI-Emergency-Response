namespace EmergencyResponse.Application.Media;

public interface IIncidentMediaStorage
{
    Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves content under a caller-supplied key. Overwrites any existing file at that key.
    /// </summary>
    Task SaveWithKeyAsync(
        string key,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}