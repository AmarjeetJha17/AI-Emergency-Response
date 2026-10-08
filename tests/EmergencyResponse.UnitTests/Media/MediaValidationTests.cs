using EmergencyResponse.Application.Media;

namespace EmergencyResponse.UnitTests.Media;

/// <summary>
/// Security validation tests for media upload.
/// Covers the four cases required by task 4.23:
///   - Unsupported video type → rejected
///   - Video > max size → rejected
///   - Unsupported image type → rejected
///   - Image > max size → rejected
/// </summary>
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class MediaValidationTests
{
    // ----------------------------------------------------------------
    // Video validation
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("video/mp4")]
    [InlineData("video/webm")]
    [InlineData("video/quicktime")]
    public void Video_SupportedContentType_DoesNotThrow(string contentType)
    {
        // Should not throw for supported types within size limit.
        var exception = Record.Exception(
            () => IncidentVideoValidation.Validate(
                length: 1024,
                contentType: contentType));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("video/avi")]
    [InlineData("video/x-msvideo")]
    [InlineData("video/mkv")]
    [InlineData("application/octet-stream")]
    [InlineData("image/jpeg")]
    [InlineData("text/plain")]
    public void Video_UnsupportedContentType_Throws(string contentType)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => IncidentVideoValidation.Validate(
                length: 1024,
                contentType: contentType));

        Assert.Equal("Unsupported video format.", ex.Message);
    }

    [Fact]
    public void Video_ExceedsMaxSize_Throws()
    {
        var overLimit = IncidentVideoValidation.MaxVideoBytes + 1;

        var ex = Assert.Throws<InvalidOperationException>(
            () => IncidentVideoValidation.Validate(
                length: overLimit,
                contentType: "video/mp4"));

        Assert.Equal("The video exceeds the maximum allowed size.", ex.Message);
    }

    [Fact]
    public void Video_ExactlyAtMaxSize_DoesNotThrow()
    {
        // The boundary value itself must be accepted.
        var exception = Record.Exception(
            () => IncidentVideoValidation.Validate(
                length: IncidentVideoValidation.MaxVideoBytes,
                contentType: "video/mp4"));

        Assert.Null(exception);
    }

    [Fact]
    public void Video_ZeroLength_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => IncidentVideoValidation.Validate(
                length: 0,
                contentType: "video/mp4"));

        Assert.Equal("The uploaded video is empty.", ex.Message);
    }

    [Fact]
    public void Video_NegativeLength_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => IncidentVideoValidation.Validate(
                length: -1,
                contentType: "video/mp4"));
    }

    [Fact]
    public void Video_ContentType_CheckIsCaseInsensitive()
    {
        // Content types from browsers can arrive in any casing.
        var exception = Record.Exception(
            () => IncidentVideoValidation.Validate(
                length: 512,
                contentType: "VIDEO/MP4"));

        Assert.Null(exception);
    }

    // ----------------------------------------------------------------
    // Image validation
    // ----------------------------------------------------------------

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public void Image_SupportedContentType_DoesNotThrow(string contentType)
    {
        var exception = Record.Exception(
            () => IncidentMediaValidation.ValidateImage(
                length: 1024,
                contentType: contentType));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("image/bmp")]
    [InlineData("image/tiff")]
    [InlineData("image/svg+xml")]
    [InlineData("video/mp4")]
    [InlineData("application/octet-stream")]
    [InlineData("text/plain")]
    public void Image_UnsupportedContentType_Throws(string contentType)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => IncidentMediaValidation.ValidateImage(
                length: 1024,
                contentType: contentType));

        Assert.Equal("Unsupported image format.", ex.Message);
    }

    [Fact]
    public void Image_ExceedsMaxSize_Throws()
    {
        var overLimit = IncidentMediaValidation.MaxImageBytes + 1;

        var ex = Assert.Throws<InvalidOperationException>(
            () => IncidentMediaValidation.ValidateImage(
                length: overLimit,
                contentType: "image/jpeg"));

        Assert.Equal("The image exceeds the maximum allowed size.", ex.Message);
    }

    [Fact]
    public void Image_ExactlyAtMaxSize_DoesNotThrow()
    {
        var exception = Record.Exception(
            () => IncidentMediaValidation.ValidateImage(
                length: IncidentMediaValidation.MaxImageBytes,
                contentType: "image/jpeg"));

        Assert.Null(exception);
    }

    [Fact]
    public void Image_ZeroLength_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => IncidentMediaValidation.ValidateImage(
                length: 0,
                contentType: "image/jpeg"));

        Assert.Equal("The uploaded image is empty.", ex.Message);
    }

    [Fact]
    public void Image_NegativeLength_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => IncidentMediaValidation.ValidateImage(
                length: -1,
                contentType: "image/jpeg"));
    }

    [Fact]
    public void Image_ContentType_CheckIsCaseInsensitive()
    {
        var exception = Record.Exception(
            () => IncidentMediaValidation.ValidateImage(
                length: 512,
                contentType: "IMAGE/JPEG"));

        Assert.Null(exception);
    }

    // ----------------------------------------------------------------
    // Constant sanity checks
    // ----------------------------------------------------------------

    [Fact]
    public void Video_MaxVideoBytes_Is_100MB()
    {
        Assert.Equal(100L * 1024 * 1024, IncidentVideoValidation.MaxVideoBytes);
    }

    [Fact]
    public void Image_MaxImageBytes_Is_10MB()
    {
        Assert.Equal(10L * 1024 * 1024, IncidentMediaValidation.MaxImageBytes);
    }
}
