using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using EmergencyResponse.Application.Knowledge;

namespace EmergencyResponse.Infrastructure.Knowledge;

/// <summary>Stores knowledge source files in a private Azure Blob container using workload identity.</summary>
public sealed class AzureBlobKnowledgeDocumentStorage : IKnowledgeDocumentStorage
{
    private readonly BlobContainerClient _container;

    public AzureBlobKnowledgeDocumentStorage(
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
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var extension = KnowledgeDocumentValidation.GetExtension(fileName);
        var key = $"{Guid.NewGuid():N}{extension}";
        ValidateKey(key);

        await _container.GetBlobClient(key).UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(contentType)
                        ? "application/octet-stream"
                        : contentType
                }
            },
            cancellationToken);
        return key;
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
            throw new InvalidOperationException("The knowledge document storage key is invalid.");
        }
    }
}
