using EmergencyResponse.Infrastructure.Media;

namespace EmergencyResponse.UnitTests.Media;

/// <summary>
/// Tests for FfmpegVideoFrameExtractor.CalculateTimestamps (internal static).
/// Covers the four cases required by task 4.23.
/// </summary>
public sealed class CalculateTimestampsTests
{
    // ----------------------------------------------------------------
    // MaximumFrames = 0 → always empty, regardless of duration
    // ----------------------------------------------------------------

    [Fact]
    public void MaximumFrames_Zero_Returns_Empty()
    {
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 60,
            maximumFrames: 0);

        Assert.Empty(result);
    }

    [Fact]
    public void MaximumFrames_Zero_With_Long_Video_Returns_Empty()
    {
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 3600,
            maximumFrames: 0);

        Assert.Empty(result);
    }

    // ----------------------------------------------------------------
    // 1-second video → at most 1 frame
    // ----------------------------------------------------------------

    [Fact]
    public void OneSecondVideo_ReturnsAtMostOneFrame()
    {
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 1.0,
            maximumFrames: 8);

        Assert.True(
            result.Count <= 1,
            $"Expected ≤ 1 frame for a 1-second video, got {result.Count}.");
    }

    [Fact]
    public void OneSecondVideo_SingleFrame_IsAtTimestampZero()
    {
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 1.0,
            maximumFrames: 8);

        Assert.Single(result);
        Assert.Equal(0.0, result[0]);
    }

    // ----------------------------------------------------------------
    // 10-second video → at most 8 frames
    // ----------------------------------------------------------------

    [Fact]
    public void TenSecondVideo_AtMostEightFrames()
    {
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 10.0,
            maximumFrames: 8);

        Assert.True(
            result.Count <= 8,
            $"Expected ≤ 8 frames for a 10-second video, got {result.Count}.");
    }

    [Fact]
    public void TenSecondVideo_ExactlyEightFrames_WhenMaxAllows()
    {
        // ceil(10) = 10 candidates, capped to MaximumFrames = 8.
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 10.0,
            maximumFrames: 8);

        Assert.Equal(8, result.Count);
    }

    // ----------------------------------------------------------------
    // 60-second video → exactly 8 frames
    // ----------------------------------------------------------------

    [Fact]
    public void SixtySecondVideo_ExactlyEightFrames()
    {
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 60.0,
            maximumFrames: 8);

        Assert.Equal(8, result.Count);
    }

    [Fact]
    public void SixtySecondVideo_FirstTimestamp_IsZero()
    {
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 60.0,
            maximumFrames: 8);

        Assert.Equal(0.0, result[0]);
    }

    [Fact]
    public void SixtySecondVideo_LastTimestamp_DoesNotExceedDuration()
    {
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 60.0,
            maximumFrames: 8);

        Assert.True(
            result[result.Count - 1] <= 60.0,
            $"Last timestamp {result[result.Count - 1]} must not exceed 60 s.");
    }

    [Fact]
    public void SixtySecondVideo_TimestampsAreStrictlyAscending()
    {
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 60.0,
            maximumFrames: 8);

        for (var i = 1; i < result.Count; i++)
        {
            Assert.True(
                result[i] > result[i - 1],
                $"Timestamp[{i}] ({result[i]:F3}) must be > Timestamp[{i - 1}] ({result[i - 1]:F3}).");
        }
    }

    // ----------------------------------------------------------------
    // Additional edge cases
    // ----------------------------------------------------------------

    [Fact]
    public void MaximumFrames_One_AlwaysReturnsTimestampZero()
    {
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 30.0,
            maximumFrames: 1);

        Assert.Single(result);
        Assert.Equal(0.0, result[0]);
    }

    [Fact]
    public void SubSecondVideo_ReturnsAtMostOneFrame()
    {
        // ceil(0.5) = 1
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 0.5,
            maximumFrames: 8);

        Assert.True(result.Count <= 1);
    }

    [Fact]
    public void AllTimestamps_AreNonNegative()
    {
        var result = FfmpegVideoFrameExtractor.CalculateTimestamps(
            durationSeconds: 45.0,
            maximumFrames: 8);

        foreach (var ts in result)
        {
            Assert.True(ts >= 0, $"Timestamp {ts} must not be negative.");
        }
    }
}
