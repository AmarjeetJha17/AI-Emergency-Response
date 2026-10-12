using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.DTOs.Incidents;
using Xunit;

namespace EmergencyResponse.IntegrationTests.Health;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Collection("SQL Server integration database")]
public sealed class ReprocessingApiTests
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ReprocessingApiTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
    }

    private async Task<string> LoginAdminAsync()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest
            {
                Email = "admin@test.local",
                Password = "Admin@Test1!"
            });
        await loginResp.EnsureSuccessWithBodyAsync();

        var auth = await loginResp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        return auth!.AccessToken;
    }

    private async Task<string> RegisterAndLoginCitizenAsync()
    {
        var email = $"citizen.{Guid.NewGuid():N}@test.local";
        const string password = "Test@123!";

        var registerResp = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest
            {
                FirstName = "Citizen",
                LastName = "User",
                Email = email,
                Password = password
            });
        await registerResp.EnsureSuccessWithBodyAsync();

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = password });
        await loginResp.EnsureSuccessWithBodyAsync();

        var auth = await loginResp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        return auth!.AccessToken;
    }

    private async Task<Guid> CreateIncidentAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var typesResp = await _client.GetAsync("/api/incident-types");
        await typesResp.EnsureSuccessWithBodyAsync();
        var types = await typesResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var typeId = types[0].GetProperty("id").GetGuid();

        var resp = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest
        {
            Title = "Test Reprocess Incident",
            Description = "Incident to test manual reprocessing endpoints",
            IncidentTypeId = typeId,
            Latitude = 12.9716M,
            Longitude = 77.5946M
        });
        await resp.EnsureSuccessWithBodyAsync();

        var created = await resp.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);
        return created!.Id;
    }

    [Fact]
    public async Task ReprocessAI_WithoutAuth_ShouldReturnUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsync($"/api/incidents/{Guid.NewGuid()}/ai/reprocess", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReprocessAI_WithCitizenRole_ShouldReturnForbidden()
    {
        var token = await RegisterAndLoginCitizenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync($"/api/incidents/{Guid.NewGuid()}/ai/reprocess", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReprocessAI_WithAdmin_WhenIncidentNotFound_ShouldReturnNotFound()
    {
        var token = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync($"/api/incidents/{Guid.NewGuid()}/ai/reprocess", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task ReprocessAI_WithAdmin_WhenIncidentExists_ShouldReturnAccepted()
    {
        var token = await LoginAdminAsync();
        var incidentId = await CreateIncidentAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsync($"/api/incidents/{incidentId}/ai/reprocess", null);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task ReprocessDuplicates_WithAdmin_WhenIncidentExists_ShouldReturnOk()
    {
        var token = await LoginAdminAsync();
        var incidentId = await CreateIncidentAsync(token);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.PostAsync($"/api/incidents/{incidentId}/duplicates/reprocess", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReanalyzeSituation_WithoutAuth_ShouldReturnUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;
        var response = await _client.PostAsync($"/api/situations/{Guid.NewGuid()}/reanalyze", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReanalyzeSituation_WithCitizenRole_ShouldReturnForbidden()
    {
        var token = await RegisterAndLoginCitizenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync($"/api/situations/{Guid.NewGuid()}/reanalyze", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ReanalyzeSituation_WithAdmin_WhenNotFound_ShouldReturnNotFound()
    {
        var token = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsync($"/api/situations/{Guid.NewGuid()}/reanalyze", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
