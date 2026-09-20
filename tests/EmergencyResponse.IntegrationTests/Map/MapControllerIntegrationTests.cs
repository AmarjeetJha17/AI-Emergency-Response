using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Map;

namespace EmergencyResponse.IntegrationTests.Map;

public sealed class MapControllerIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public MapControllerIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
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
        registerResp.EnsureSuccessStatusCode();

        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest { Email = email, Password = password });
        loginResp.EnsureSuccessStatusCode();

        var auth = await loginResp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        return auth!.AccessToken;
    }

    private async Task<string> LoginAdminAsync()
    {
        var loginResp = await _client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest
            {
                Email = "admin@test.local",
                Password = "Admin@Test1!"
            });
        loginResp.EnsureSuccessStatusCode();

        var auth = await loginResp.Content.ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);
        return auth!.AccessToken;
    }

    [Fact]
    public async Task GetIncidents_Unauthenticated_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/map/incidents");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetIncidents_AsCitizen_ReturnsForbidden()
    {
        var token = await RegisterAndLoginCitizenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/map/incidents");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetIncidents_AsAdmin_ReturnsOkAndExcludesPII()
    {
        // 1. Login as Admin
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // 2. Get first incident type
        var typesResp = await _client.GetAsync("/api/incident-types");
        typesResp.EnsureSuccessStatusCode();
        var types = await typesResp.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOptions);
        var typeId = Guid.Parse(types![0].GetProperty("id").GetString()!);

        // 3. Create an incident with location
        var createRequest = new CreateIncidentRequest
        {
            Title = "Live Map Verification Emergency",
            Description = "Testing live map operational projection with geographic marker coordinates.",
            IncidentTypeId = typeId,
            Latitude = 18.5204m,
            Longitude = 73.8567m,
            Address = "Shivaji Nagar",
            City = "Pune"
        };

        var createResp = await _client.PostAsJsonAsync("/api/incidents", createRequest);
        createResp.EnsureSuccessStatusCode();

        // 4. Request map incidents endpoint
        var mapResp = await _client.GetAsync("/api/map/incidents");
        Assert.Equal(HttpStatusCode.OK, mapResp.StatusCode);

        var rawJson = await mapResp.Content.ReadAsStringAsync();
        var incidents = JsonSerializer.Deserialize<List<EmergencyMapIncident>>(rawJson, JsonOptions);

        Assert.NotNull(incidents);
        Assert.NotEmpty(incidents);

        var match = incidents!.FirstOrDefault(x => x.Title == "Live Map Verification Emergency");
        Assert.NotNull(match);
        Assert.Equal(18.5204, match!.Latitude, precision: 3);
        Assert.Equal(73.8567, match!.Longitude, precision: 3);
        Assert.Equal("Shivaji Nagar", match!.Address);

        // 5. Verify NO Citizen PII or Raw AI Output in payload
        using var jsonDoc = JsonDocument.Parse(rawJson);
        var firstElement = jsonDoc.RootElement.EnumerateArray().First(x => x.GetProperty("title").GetString() == "Live Map Verification Emergency");

        Assert.False(firstElement.TryGetProperty("reporterUserId", out _));
        Assert.False(firstElement.TryGetProperty("reporterEmail", out _));
        Assert.False(firstElement.TryGetProperty("reporterPhone", out _));
        Assert.False(firstElement.TryGetProperty("rawOutputJson", out _));
    }
}
