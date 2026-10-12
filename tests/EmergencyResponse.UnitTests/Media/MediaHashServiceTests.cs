using System.Security.Cryptography;
using EmergencyResponse.Application.Media;
using Moq;

namespace EmergencyResponse.UnitTests.Media;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class MediaHashServiceTests
{
    [Fact]
    public async Task ComputeSha256Async_ForSeekableMockStream_HashesWholeStreamAndRestoresPosition()
    {
        var content = System.Text.Encoding.UTF8.GetBytes("media payload");
        var stream = CreateMockStream(content, canSeek: true, initialPosition: 4);
        var expected = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        var actual = await MediaHashService.ComputeSha256Async(stream.Object);

        Assert.Equal(expected, actual);
        Assert.Equal(0, stream.Object.Position);
        stream.VerifySet(item => item.Position = 0, Times.Exactly(2));
    }

    [Fact]
    public async Task ComputeSha256Async_ForNonSeekableMockStream_HashesFromCurrentStreamPosition()
    {
        var content = System.Text.Encoding.UTF8.GetBytes("one-pass media payload");
        var stream = CreateMockStream(content, canSeek: false, initialPosition: 0);
        var expected = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        var actual = await MediaHashService.ComputeSha256Async(stream.Object);

        Assert.Equal(expected, actual);
        stream.VerifySet(item => item.Position = It.IsAny<long>(), Times.Never);
        stream.VerifyGet(item => item.Position, Times.Never);
    }

    private static Mock<Stream> CreateMockStream(
        byte[] content,
        bool canSeek,
        long initialPosition)
    {
        var mock = new Mock<Stream>();
        var offset = checked((int)initialPosition);
        var position = initialPosition;

        mock.SetupGet(stream => stream.CanSeek).Returns(canSeek);
        mock.SetupGet(stream => stream.CanRead).Returns(true);
        mock.SetupGet(stream => stream.Position).Returns(() => position);
        mock.SetupSet(stream => stream.Position = It.IsAny<long>())
            .Callback<long>(value =>
            {
                position = value;
                offset = checked((int)value);
            });
        mock.Setup(stream => stream.ReadAsync(
                It.IsAny<Memory<byte>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Memory<byte> buffer, CancellationToken _) =>
            {
                var count = Math.Min(buffer.Length, content.Length - offset);
                content.AsMemory(offset, count).CopyTo(buffer);
                offset += count;
                position = offset;
                return ValueTask.FromResult(count);
            });
        mock.Setup(stream => stream.ReadAsync(
                It.IsAny<byte[]>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .Returns((byte[] buffer, int bufferOffset, int count, CancellationToken _) =>
            {
                var bytesRead = Math.Min(count, content.Length - offset);
                Array.Copy(content, offset, buffer, bufferOffset, bytesRead);
                offset += bytesRead;
                position = offset;
                return Task.FromResult(bytesRead);
            });

        return mock;
    }
}
