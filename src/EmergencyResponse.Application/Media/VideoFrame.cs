namespace EmergencyResponse.Application.Media;

public sealed record VideoFrame
{
    public int FrameNumber { get; init; }

    public double TimestampSeconds { get; init; }

    public byte[] ImageBytes { get; init; } = [];
}
