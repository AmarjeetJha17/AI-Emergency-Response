namespace EmergencyResponse.Application.Media;

public interface IVideoFrameExtractor
{
    Task<IReadOnlyList<VideoFrame>> ExtractRepresentativeFramesAsync(
        Stream videoStream,
        CancellationToken cancellationToken = default);
}
