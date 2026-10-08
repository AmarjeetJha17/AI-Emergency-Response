using System.Security.Cryptography;

using EmergencyResponse.Application.Security;
using EmergencyResponse.Application.Security.FileSecurity;

using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Security.FileSecurity;

public sealed class FileSecurityService : IFileSecurityService
{
    private static readonly IReadOnlyDictionary<string, FileType> FileTypes =
        new Dictionary<string, FileType>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new("image/jpeg", 10 * 1024 * 1024, [0xFF, 0xD8, 0xFF]),
            [".jpeg"] = new("image/jpeg", 10 * 1024 * 1024, [0xFF, 0xD8, 0xFF]),
            [".png"] = new("image/png", 10 * 1024 * 1024, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]),
            [".webp"] = new("image/webp", 10 * 1024 * 1024, [0x52, 0x49, 0x46, 0x46]),
            [".gif"] = new("image/gif", 10 * 1024 * 1024, [0x47, 0x49, 0x46, 0x38]),
            [".mp4"] = new("video/mp4", 100 * 1024 * 1024, [0x00, 0x00, 0x00]),
            [".webm"] = new("video/webm", 100 * 1024 * 1024, [0x1A, 0x45, 0xDF, 0xA3]),
            [".mov"] = new("video/quicktime", 100 * 1024 * 1024, [0x00, 0x00, 0x00]),
            [".pdf"] = new("application/pdf", 100 * 1024 * 1024, [0x25, 0x50, 0x44, 0x46]),
            [".docx"] = new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", 100 * 1024 * 1024, [0x50, 0x4B]),
            [".txt"] = new("text/plain", 100 * 1024 * 1024, []),
            [".md"] = new("text/markdown", 100 * 1024 * 1024, [])
        };

    private readonly IMalwareScanner _malwareScanner;
    private readonly SecurityOptions _options;

    public FileSecurityService(
        IMalwareScanner malwareScanner,
        IOptions<SecurityOptions> options)
    {
        _malwareScanner = malwareScanner;
        _options = options.Value;
    }

    public async Task<FileValidationResult> ValidateAsync(
        Stream content,
        string fileName,
        string declaredContentType,
        long declaredLength,
        CancellationToken cancellationToken = default)
    {
        if (content is null || !content.CanRead)
        {
            return FileValidationResult.Invalid(
                "The uploaded file cannot be read.");
        }

        if (declaredLength <= 0)
        {
            return FileValidationResult.Invalid(
                "The uploaded file is empty.");
        }

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension) ||
            fileName.Contains("..", StringComparison.Ordinal) ||
            fileName.Any(char.IsControl) ||
            fileName.ContainsAny(['/', '\\', ':']))
        {
            return FileValidationResult.Invalid(
                "The uploaded filename is invalid.");
        }

        if (!FileTypes.TryGetValue(extension, out var fileType))
        {
            return FileValidationResult.Invalid(
                "The uploaded file type is not supported.");
        }

        var contentTypeMatches =
            string.Equals(
                fileType.ContentType,
                declaredContentType,
                StringComparison.OrdinalIgnoreCase) ||
            (string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase) &&
             string.Equals(declaredContentType, "text/plain", StringComparison.OrdinalIgnoreCase));

        if (!contentTypeMatches)
        {
            return FileValidationResult.Invalid(
                "The declared content type does not match the file extension.");
        }

        if (declaredLength > fileType.MaximumBytes)
        {
            return FileValidationResult.Invalid(
                "The uploaded file exceeds the maximum allowed size.");
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        var header = new byte[Math.Max(fileType.Signature.Length, 12)];
        var read = await content.ReadAsync(
            header.AsMemory(),
            cancellationToken);

        if (fileType.Signature.Length > 0 &&
            !StartsWith(header.AsSpan(0, read), fileType.Signature))
        {
            return FileValidationResult.Invalid(
                "The file signature does not match the declared type.");
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        if (_options.MalwareScanningEnabled)
        {
            var scan = await _malwareScanner.ScanAsync(
                content,
                cancellationToken);

            if (!scan.IsAvailable &&
                _options.FailClosedWhenScannerUnavailable)
            {
                return FileValidationResult.Invalid(
                    "Malware scanning is unavailable.");
            }

            if (scan.IsAvailable && !scan.IsClean)
            {
                return FileValidationResult.Invalid(
                    "The uploaded file failed malware scanning.");
            }
        }

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        _ = await ComputeHashAsync(content, cancellationToken);

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return FileValidationResult.Valid(
            extension.ToLowerInvariant());
    }

    private static bool StartsWith(
        ReadOnlySpan<byte> content,
        IReadOnlyList<byte> signature)
    {
        if (content.Length < signature.Count)
        {
            return false;
        }

        for (var index = 0; index < signature.Count; index++)
        {
            if (content[index] != signature[index])
            {
                return false;
            }
        }

        return true;
    }

    private static async Task<byte[]> ComputeHashAsync(
        Stream content,
        CancellationToken cancellationToken)
    {
        using var sha256 = SHA256.Create();
        return await sha256.ComputeHashAsync(
            content,
            cancellationToken);
    }

    private sealed record FileType(
        string ContentType,
        long MaximumBytes,
        byte[] Signature);
}
