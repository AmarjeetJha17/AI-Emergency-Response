namespace EmergencyResponse.Application.Media;

public static class IncidentVideoValidation
{
    public const long MaxVideoBytes = 100 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes =
    [
        "video/mp4",
        "video/webm",
        "video/quicktime"
    ];

    public static void Validate(
        long length,
        string contentType)
    {
        if (length <= 0)
        {
            throw new InvalidOperationException(
                "The uploaded video is empty.");
        }

        if (length > MaxVideoBytes)
        {
            throw new InvalidOperationException(
                "The video exceeds the maximum allowed size.");
        }

        if (!AllowedContentTypes.Contains(
                contentType.ToLowerInvariant()))
        {
            throw new InvalidOperationException(
                "Unsupported video format.");
        }
    }
}