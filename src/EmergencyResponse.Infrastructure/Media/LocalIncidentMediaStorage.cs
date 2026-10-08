using EmergencyResponse.Application.Media;

using Microsoft.Extensions.Configuration;

namespace EmergencyResponse.Infrastructure.Media;

public sealed class LocalIncidentMediaStorage : IIncidentMediaStorage
{
    private readonly string _rootPath;

    public LocalIncidentMediaStorage(
        IConfiguration configuration)
    {
        _rootPath =
            configuration["IncidentMediaStorage:RootPath"]
            ?? Path.Combine(
                AppContext.BaseDirectory,
                "private-media");

        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default)
    {
        var key =
            $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";

        var fullPath =
            Path.Combine(_rootPath, key);

        await using var output =
            new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                useAsync: true);

        await content.CopyToAsync(
            output,
            cancellationToken);

        return key;
    }

    public async Task SaveWithKeyAsync(
        string key,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(key);

        Directory.CreateDirectory(
            Path.GetDirectoryName(fullPath)!);

        await using var output =
            new FileStream(
                fullPath,
                FileMode.Create,        // overwrite if re-run
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                useAsync: true);

        await content.CopyToAsync(output, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveSafePath(storageKey);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "Incident media was not found.",
                storageKey);
        }

        Stream stream =
            new FileStream(
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
        var fullPath = ResolveSafePath(storageKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolveSafePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "A storage key is required.");
        }

        var root = Path.GetFullPath(_rootPath)
            .TrimEnd(Path.DirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var candidate = Path.GetFullPath(
            Path.Combine(
                _rootPath,
                key.Replace('/', Path.DirectorySeparatorChar)
                   .Replace('\\', Path.DirectorySeparatorChar)));

        if (!candidate.StartsWith(
                root,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The storage key resolves outside private media storage.");
        }

        return candidate;
    }
}