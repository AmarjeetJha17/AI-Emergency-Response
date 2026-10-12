using System.Net.Http.Json;

namespace EmergencyResponse.IntegrationTests.Infrastructure;

public static class HttpContentExtensions
{
    public static async Task EnsureSuccessWithBodyAsync(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new Xunit.Sdk.XunitException(
            $"Request failed with {(int)response.StatusCode} {response.StatusCode}. Body: {body}");
    }

    public static async Task<T> ReadJsonAsync<T>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        await response.EnsureSuccessWithBodyAsync(cancellationToken);
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidOperationException("Expected a JSON response body.");
    }
}
