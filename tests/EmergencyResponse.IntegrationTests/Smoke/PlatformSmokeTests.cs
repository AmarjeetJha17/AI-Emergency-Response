using System.Net;

namespace EmergencyResponse.IntegrationTests.Smoke;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Smoke)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Collection("SQL Server integration database")]
public sealed class PlatformSmokeTests : IClassFixture<EmergencyResponseWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformSmokeTests(EmergencyResponseWebApplicationFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task LivenessAndIncidentApiContract_AreAvailable()
    {
        var health = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
        Assert.True(health.Headers.Contains("X-Correlation-ID"));

        var unauthorized = await _client.GetAsync("/api/incidents");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

        var createWithoutBody = await _client.PostAsync("/api/incidents", null);
        Assert.Equal(HttpStatusCode.Unauthorized, createWithoutBody.StatusCode);
    }
}
