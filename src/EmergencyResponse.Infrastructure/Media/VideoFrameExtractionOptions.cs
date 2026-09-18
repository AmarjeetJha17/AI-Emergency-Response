namespace EmergencyResponse.Infrastructure.Media;

public sealed class VideoFrameExtractionOptions
{
    public int MaximumFrames { get; set; } = 8;

    public int JpegQuality { get; set; } = 85;

    public int Width { get; set; } = 1280;
}
