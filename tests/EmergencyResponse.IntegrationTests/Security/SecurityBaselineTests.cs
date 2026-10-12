using System.Net;
using System.Net.Http.Json;

namespace EmergencyResponse.IntegrationTests.Security;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Security)]
[Collection("SQL Server integration database")]
public sealed class SecurityBaselineTests

{
    private readonly HttpClient _client;

    public SecurityBaselineTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task Api_responses_include_baseline_security_headers()
    {
        using var response = await _client.GetAsync("/health/live");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
    }

    [Fact]
    public async Task Protected_copilot_endpoint_rejects_anonymous_requests()
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/copilot/query",
            new { question = "What is the current situation?" });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}
