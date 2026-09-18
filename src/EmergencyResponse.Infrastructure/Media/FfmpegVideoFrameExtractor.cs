using System.Diagnostics;

using EmergencyResponse.Application.Media;

using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Media;

public sealed class FfmpegVideoFrameExtractor : IVideoFrameExtractor
{
    private readonly VideoFrameExtractionOptions _options;

    public FfmpegVideoFrameExtractor(
        IOptions<VideoFrameExtractionOptions> options)
    {
        _options = options.Value;
    }

    private static readonly TimeSpan FfmpegTimeout = TimeSpan.FromMinutes(2);

    public async Task<IReadOnlyList<VideoFrame>>
        ExtractRepresentativeFramesAsync(
            Stream videoStream,
            CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(FfmpegTimeout);
        using var linkedCts = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);
        var ct = linkedCts.Token;

        var tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "EmergencyResponse",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(tempDirectory);

        var inputPath = Path.Combine(tempDirectory, "input");

        try
        {
            await using (var file = new FileStream(
                inputPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                64 * 1024,
                useAsync: true))
            {
                await videoStream.CopyToAsync(file, ct);
            }

            var duration = await GetDurationAsync(
                inputPath,
                ct);

            if (duration <= 0)
            {
                throw new InvalidOperationException(
                    "Unable to determine video duration.");
            }

            var frameTimestamps = CalculateTimestamps(
                duration,
                _options.MaximumFrames);

            var frames = new List<VideoFrame>(frameTimestamps.Count);

            for (var i = 0; i < frameTimestamps.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var timestamp = frameTimestamps[i];

                var outputPath = Path.Combine(
                    tempDirectory,
                    $"frame-{i:D3}.jpg");

                await ExtractFrameAsync(
                    inputPath,
                    outputPath,
                    timestamp,
                    ct);

                var bytes = await File.ReadAllBytesAsync(
                    outputPath,
                    ct);

                frames.Add(new VideoFrame
                {
                    FrameNumber = i,
                    TimestampSeconds = timestamp,
                    ImageBytes = bytes
                });
            }

            return frames;
        }
        finally
        {
            TryDeleteDirectory(tempDirectory);
        }
    }

    private static async Task<double> GetDurationAsync(
        string inputPath,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffprobe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("error");
        startInfo.ArgumentList.Add("-show_entries");
        startInfo.ArgumentList.Add("format=duration");
        startInfo.ArgumentList.Add("-of");
        startInfo.ArgumentList.Add("default=noprint_wrappers=1:nokey=1");
        startInfo.ArgumentList.Add(inputPath);

        using var process = new Process { StartInfo = startInfo };

        process.Start();

        var output = await process.StandardOutput
            .ReadToEndAsync(cancellationToken);

        var error = await process.StandardError
            .ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"ffprobe failed: {error}");
        }

        if (!double.TryParse(
                output.Trim(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var duration))
        {
            throw new InvalidOperationException(
                "Invalid video duration returned by ffprobe.");
        }

        return duration;
    }

    private static async Task ExtractFrameAsync(
        string inputPath,
        string outputPath,
        double timestampSeconds,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-y");
        startInfo.ArgumentList.Add("-ss");
        startInfo.ArgumentList.Add(
            timestampSeconds.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add("-frames:v");
        startInfo.ArgumentList.Add("1");
        startInfo.ArgumentList.Add("-vf");
        startInfo.ArgumentList.Add("scale=1280:-1");
        startInfo.ArgumentList.Add("-q:v");
        startInfo.ArgumentList.Add("3");
        startInfo.ArgumentList.Add(outputPath);

        using var process = new Process { StartInfo = startInfo };

        process.Start();

        var error = await process.StandardError
            .ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"FFmpeg frame extraction failed: {error}");
        }
    }

    internal static IReadOnlyList<double> CalculateTimestamps(
        double durationSeconds,
        int maximumFrames)
    {
        if (maximumFrames <= 0)
        {
            return [];
        }

        var frameCount = (int)Math.Min(
            maximumFrames,
            Math.Max(1, Math.Ceiling(durationSeconds)));

        if (frameCount == 1)
        {
            return [0];
        }

        var result = new List<double>(frameCount);

        for (var i = 0; i < frameCount; i++)
        {
            var ratio = (double)i / (frameCount - 1);
            result.Add(ratio * Math.Max(0, durationSeconds - 0.1));
        }

        return result;
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Cleanup failure should not hide the original processing error.
        }
    }
}
