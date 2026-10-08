using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.DTOs.Common;
using EmergencyResponse.Application.DTOs.Incidents;

namespace EmergencyResponse.IntegrationTests.Incidents;

/// <summary>
/// Smoke tests for the /api/incidents endpoints.
/// </summary>
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Collection("SQL Server integration database")]
public sealed class IncidentEndpointTests
    : IClassFixture<EmergencyResponseWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IncidentEndpointTests(EmergencyResponseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<string> RegisterAndLoginAsync(
        string? email = null,
        string password = "Test@123!")
    {
        email ??= $"user.{Guid.NewGuid():N}@test.local";

        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest
            {
                FirstName = "Test",
                LastName  = "User",
                Email     = email,
                Password  = password
            });

        var loginResp = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = email, Password = password });

        var auth = await loginResp.Content
            .ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);

        return auth!.AccessToken;
    }

    private async Task<Guid> GetFirstIncidentTypeIdAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var resp = await _client.GetAsync("/api/incident-types");
        resp.EnsureSuccessStatusCode();

        var types = await resp.Content
            .ReadFromJsonAsync<List<JsonElement>>(JsonOptions);

        return Guid.Parse(types![0].GetProperty("id").GetString()!);
    }

    // ── GET /api/incidents (unauthenticated) ──────────────────────────────

    [Fact]
    public async Task GetIncidents_Unauthenticated_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/incidents");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ── POST /api/incidents ───────────────────────────────────────────────

    [Fact]
    public async Task CreateIncident_Authenticated_Returns201()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var typeId = await GetFirstIncidentTypeIdAsync(token);

        var request = new CreateIncidentRequest
        {
            Title           = "Flood near Old Bridge",
            Description     = "Heavy rain has flooded the road and several vehicles are stranded.",
            IncidentTypeId  = typeId,
            Latitude        = 28.6139m,
            Longitude       = 77.2090m,
            Address         = "Old Bridge Road",
            City            = "New Delhi"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/incidents", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<IncidentResponse>(JsonOptions);

        Assert.NotNull(body);
        Assert.NotEmpty(body!.IncidentNumber);
        Assert.Equal("Flood near Old Bridge", body.Title);
        Assert.Equal("Submitted", body.Status);
        Assert.Equal("New Delhi", body.City);
    }

    [Fact]
    public async Task CreateIncident_MissingTitle_Returns400()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var typeId = await GetFirstIncidentTypeIdAsync(token);

        var request = new
        {
            Title           = "",           // invalid
            Description     = "Some valid description here that is long enough.",
            IncidentTypeId  = typeId,
            Latitude        = 28.6m,
            Longitude       = 77.2m
        };

        var response = await _client.PostAsJsonAsync(
            "/api/incidents", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateIncident_InvalidLatitude_Returns400()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var typeId = await GetFirstIncidentTypeIdAsync(token);

        var request = new
        {
            Title           = "Valid title",
            Description     = "Valid description that meets the minimum length.",
            IncidentTypeId  = typeId,
            Latitude        = 999m,         // invalid
            Longitude       = 77.2m
        };

        var response = await _client.PostAsJsonAsync(
            "/api/incidents", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── GET /api/incidents/{id} ───────────────────────────────────────────

    [Fact]
    public async Task GetIncidentById_Exists_Returns200()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var typeId = await GetFirstIncidentTypeIdAsync(token);

        var created = await (await _client.PostAsJsonAsync(
            "/api/incidents",
            new CreateIncidentRequest
            {
                Title          = "Road accident on highway",
                Description    = "Multiple vehicle collision blocking both lanes of the highway.",
                IncidentTypeId = typeId,
                Latitude       = 28.5m,
                Longitude      = 77.1m
            })).Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);

        var response = await _client.GetAsync(
            $"/api/incidents/{created!.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<IncidentResponse>(JsonOptions);

        Assert.Equal(created.Id, body!.Id);
        Assert.Equal("Road accident on highway", body.Title);
    }

    [Fact]
    public async Task GetIncidentById_NotFound_Returns404()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync(
            $"/api/incidents/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── GET /api/incidents (paged) ────────────────────────────────────────

    [Fact]
    public async Task GetIncidents_Authenticated_ReturnsPagedResult()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync(
            "/api/incidents?page=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<PagedResult<IncidentListItem>>(JsonOptions);

        Assert.NotNull(body);
        Assert.True(body!.Page >= 1);
        Assert.True(body.PageSize >= 1);
    }

    // ── Incident type lookup ──────────────────────────────────────────────

    [Fact]
    public async Task GetIncidentTypes_Authenticated_ReturnsSeededTypes()
    {
        var token = await RegisterAndLoginAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/incident-types");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var types = await response.Content
            .ReadFromJsonAsync<List<JsonElement>>(JsonOptions);

        // DatabaseSeeder seeds 10 incident types
        Assert.NotNull(types);
        Assert.True(types!.Count >= 1);
    }
}
