namespace EmergencyResponse.Application.Media;

public static class IncidentMediaValidation
{
    public const long MaxImageBytes = 10 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    ];

    public static void ValidateImage(
        long length,
        string contentType)
    {
        if (length <= 0)
        {
            throw new InvalidOperationException(
                "The uploaded image is empty.");
        }

        if (length > MaxImageBytes)
        {
            throw new InvalidOperationException(
                "The image exceeds the maximum allowed size.");
        }

        if (!AllowedContentTypes.Contains(
                contentType.ToLowerInvariant()))
        {
            throw new InvalidOperationException(
                "Unsupported image format.");
        }
    }
}