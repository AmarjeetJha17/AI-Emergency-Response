using System.Diagnostics;
using System.Net.Http.Headers;
using EmergencyResponse.LoadTests.Infrastructure;
using NBomber.CSharp;
using NBomber.Contracts;

namespace EmergencyResponse.LoadTests.Scenarios;

public static class MediaUploadScenario
{
    public static ScenarioProps Create(LoadScenarioContext test)
    {
        if (!string.Equals(test.Options.HostMode, "LocalTestHost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The bounded media-upload workload uses the test host's isolated fake storage.");
        }

        var image = CreatePayload(test.Options.MediaImageSizeMb * 1024 * 1024, [0xff, 0xd8, 0xff]);
        var video = CreatePayload(test.Options.MediaVideoSizeMb * 1024 * 1024, [0x00, 0x00, 0x00, 0x66, 0x74, 0x79, 0x70]);
        var sent = 0;

        return Scenario.Create("media-upload-bounded", async context =>
        {
            var index = Interlocked.Increment(ref sent) - 1;
            if (index >= test.Options.MediaUploadCount) return Response.Ok(message: "Configured upload cap reached.");

            var isVideo = index >= test.Options.MediaUploadCount / 2;
            var payload = isVideo ? video : image;
            var incidentId = test.NextIncidentId();
            var path = isVideo ? $"/api/incidents/{incidentId}/media/video" : $"/api/incidents/{incidentId}/media";
            var fileName = isVideo ? $"synthetic-{index}.mp4" : $"synthetic-{index}.jpg";
            var contentType = isVideo ? "video/mp4" : "image/jpeg";
            var timer = Stopwatch.StartNew();
            var success = false;
            try
            {
                using var form = new MultipartFormDataContent();
                var file = new ByteArrayContent(payload);
                file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                form.Add(file, "file", fileName);
                using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = form };
                using var response = await test.Client.SendAsync(request, context.ScenarioCancellationToken);
                success = response.IsSuccessStatusCode;
                test.Metrics.Record(isVideo ? "media.video-upload" : "media.image-upload", timer.Elapsed, success);
                return success ? Response.Ok(statusCode: ((int)response.StatusCode).ToString()) :
                    Response.Fail(statusCode: ((int)response.StatusCode).ToString(), message: "Upload failed");
            }
            catch (Exception exception) when (exception is HttpRequestException or IOException or TaskCanceledException)
            {
                test.Metrics.Record(isVideo ? "media.video-upload" : "media.image-upload", timer.Elapsed, false);
                return Response.Fail(message: exception.GetType().Name);
            }
        });
    }

    private static byte[] CreatePayload(int sizeBytes, byte[] header)
    {
        var payload = new byte[sizeBytes];
        header.CopyTo(payload, 0);
        return payload;
    }
}
