using System.Net.Http.Json;

namespace EmergencyResponse.IntegrationTests.Infrastructure;

public static class HttpContentExtensions
{
    public static async Task<T> ReadJsonAsync<T>(
        this HttpResponseMessage response,
        CancellationToken cancellationToken = default)
    {
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken)
            ?? throw new InvalidOperationException("Expected a JSON response body.");
    }
}
