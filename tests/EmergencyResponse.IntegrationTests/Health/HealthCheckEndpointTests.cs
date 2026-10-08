using System.Net;
using Xunit;

namespace EmergencyResponse.IntegrationTests.Health;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Collection("SQL Server integration database")]
public sealed class HealthCheckEndpointTests : IClassFixture<EmergencyResponseWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthCheckEndpointTests(EmergencyResponseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task CorrelationId_WhenHeaderProvided_ShouldEchoBack()
    {
        const string customCorrelationId = "test-correlation-12345";
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", customCorrelationId);

        var response = await _client.SendAsync(request);

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        Assert.Equal(customCorrelationId, response.Headers.GetValues("X-Correlation-ID").First());
    }

    [Fact]
    public async Task CorrelationId_WhenHeaderMissing_ShouldGenerateNew()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        var generatedId = response.Headers.GetValues("X-Correlation-ID").First();
        Assert.False(string.IsNullOrWhiteSpace(generatedId));
        Assert.True(Guid.TryParse(generatedId, out _));
    }
}
