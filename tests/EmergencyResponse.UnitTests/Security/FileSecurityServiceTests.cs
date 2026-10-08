using EmergencyResponse.Application.Security;
using EmergencyResponse.Infrastructure.Security.FileSecurity;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.UnitTests.Security;

public sealed class FileSecurityServiceTests
{
    private readonly FileSecurityService _service =
        new(
            new UnavailableMalwareScanner(),
            Options.Create(new SecurityOptions
            {
                MalwareScanningEnabled = false
            }));

    [Fact]
    public async Task Valid_png_signature_is_accepted()
    {
        var content = new MemoryStream(
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
            0x00, 0x00, 0x00, 0x00
        ]);

        var result = await _service.ValidateAsync(
            content,
            "scene.png",
            "image/png",
            content.Length);

        Assert.True(result.IsValid);
        Assert.Equal(".png", result.SafeExtension);
        Assert.Equal(0, content.Position);
    }

    [Fact]
    public async Task Mismatched_signature_is_rejected()
    {
        var content = new MemoryStream([0x25, 0x50, 0x44, 0x46]);

        var result = await _service.ValidateAsync(
            content,
            "scene.png",
            "image/png",
            content.Length);

        Assert.False(result.IsValid);
        Assert.Contains("signature", result.FailureReason);
    }

    [Fact]
    public async Task Traversal_filename_is_rejected()
    {
        var content = new MemoryStream(
        [
            0xFF, 0xD8, 0xFF, 0x00
        ]);

        var result = await _service.ValidateAsync(
            content,
            "..\\private.jpg",
            "image/jpeg",
            content.Length);

        Assert.False(result.IsValid);
        Assert.Contains("filename", result.FailureReason);
    }

    [Fact]
    public async Task Enabled_scanning_fails_closed_when_scanner_is_unavailable()
    {
        var service = new FileSecurityService(
            new UnavailableMalwareScanner(),
            Options.Create(new SecurityOptions
            {
                MalwareScanningEnabled = true,
                FailClosedWhenScannerUnavailable = true
            }));
        var content = new MemoryStream(
        [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A
        ]);

        var result = await service.ValidateAsync(
            content,
            "scene.png",
            "image/png",
            content.Length);

        Assert.False(result.IsValid);
        Assert.Contains("scanning", result.FailureReason);
    }
}
