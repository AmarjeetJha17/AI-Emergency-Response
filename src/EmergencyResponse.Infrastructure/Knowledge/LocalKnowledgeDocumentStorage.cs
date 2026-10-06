using EmergencyResponse.Application.Knowledge;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Knowledge;

public sealed class LocalKnowledgeDocumentStorage
    : IKnowledgeDocumentStorage
{
    private readonly string _rootPath;

    public LocalKnowledgeDocumentStorage(
        IOptions<KnowledgeDocumentOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.StorageRootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var extension = KnowledgeDocumentValidation.GetExtension(fileName);
        var key = $"{Guid.NewGuid():N}{extension}";
        var fullPath = ResolvePath(key);

        await using var output = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            64 * 1024,
            useAsync: true);
        await content.CopyToAsync(output, cancellationToken);
        return key;
    }

    public Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "Knowledge document was not found.",
                storageKey);
        }

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            64 * 1024,
            useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolvePath(storageKey);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            Path.IsPathRooted(storageKey) ||
            storageKey.Contains("..", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The knowledge storage key is invalid.");
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storageKey));
        if (!fullPath.StartsWith(
                _rootPath + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The knowledge storage key escapes the configured root.");
        }

        return fullPath;
    }
}
