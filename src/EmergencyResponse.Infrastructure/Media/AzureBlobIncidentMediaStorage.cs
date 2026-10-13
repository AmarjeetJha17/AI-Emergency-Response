using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EmergencyResponse.Application.Media;

namespace EmergencyResponse.Infrastructure.Media;

/// <summary>Stores incident media in a private Azure Blob container using workload identity.</summary>
public sealed class AzureBlobIncidentMediaStorage : IIncidentMediaStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobIncidentMediaStorage(
        Uri blobServiceUri,
        string containerName,
        TokenCredential credential)
    {
        ArgumentNullException.ThrowIfNull(blobServiceUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);
        ArgumentNullException.ThrowIfNull(credential);

        _container = new BlobServiceClient(blobServiceUri, credential)
            .GetBlobContainerClient(containerName);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        var key = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await SaveWithKeyAsync(key, content, cancellationToken);
        return key;
    }

    public async Task SaveWithKeyAsync(
        string key,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(content);

        await _container.GetBlobClient(key).UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = "application/octet-stream"
                }
            },
            cancellationToken);
    }

    public async Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(storageKey);
        var response = await _container.GetBlobClient(storageKey)
            .DownloadStreamingAsync(cancellationToken: cancellationToken);
        return response.Value.Content;
    }

    public async Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(storageKey);
        await _container.GetBlobClient(storageKey).DeleteIfExistsAsync(
            cancellationToken: cancellationToken);
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) ||
            key.StartsWith('/') ||
            key.Contains('\\') ||
            key.Split('/').Any(segment => segment is ".." or "." or ""))
        {
            throw new InvalidOperationException("The incident media storage key is invalid.");
        }
    }
}
