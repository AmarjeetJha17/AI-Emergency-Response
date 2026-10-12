using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Situations.DTOs;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EmergencyResponse.IntegrationTests.Situations;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Collection("SQL Server integration database")]
public sealed class OperationalSituationApiTests
{
    private readonly EmergencyResponseWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OperationalSituationApiTests(IntegrationTestFixture fixture)
    {
        _factory = fixture.Factory;
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

    private async Task<Guid> GetFirstIncidentTypeIdAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await _client.GetAsync("/api/incident-types");
        await resp.EnsureSuccessWithBodyAsync();

        var types = await resp.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOptions);
        return Guid.Parse(types![0].GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task GetCommandCenter_Unauthenticated_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/situations/command-center");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCommandCenter_CitizenRole_ReturnsForbidden()
    {
        var token = await RegisterAndLoginCitizenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/situations/command-center");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenForMap_CitizenRole_ReturnsForbidden()
    {
        var token = await RegisterAndLoginCitizenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/situations/open/map");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Acknowledge_CitizenRole_ReturnsForbidden()
    {
        var token = await RegisterAndLoginCitizenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync($"/api/situations/{Guid.NewGuid()}/acknowledge", new SituationActionRequest
        {
            Notes = "Test"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CommandCenter_And_Map_And_Acknowledge_Workflow_Succeeds()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var incidentTypeId = await GetFirstIncidentTypeIdAsync(adminToken);

        // 1. Seed clustered incidents
        var createdIncidents = new List<IncidentResponse>();
        var coords = new[] { (28.6139m, 77.2090m), (28.6150m, 77.2100m), (28.6160m, 77.2110m) };

        for (var i = 0; i < 3; i++)
        {
            var createResp = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest
            {
                Title = $"Phase12 Part3 Incident {i + 1}",
                Description = $"Major gas leak near junction {i + 1}.",
                IncidentTypeId = incidentTypeId,
                Latitude = coords[i].Item1,
                Longitude = coords[i].Item2,
                Address = $"Junction {i + 1}",
                City = "New Delhi"
            });
            await createResp.EnsureSuccessWithBodyAsync();
            var inc = await createResp.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);
            Assert.NotNull(inc);
            createdIncidents.Add(inc);
        }

        // 2. Evaluate situations
        var evalResp = await _client.PostAsync("/api/situations/evaluate", null);
        await evalResp.EnsureSuccessWithBodyAsync();

        // 3. Query Command Center items
        var ccResp = await _client.GetAsync("/api/situations/command-center");
        await ccResp.EnsureSuccessWithBodyAsync();
        var ccItems = await ccResp.Content.ReadFromJsonAsync<List<SituationCommandCenterItem>>(JsonOptions);
        Assert.NotNull(ccItems);
        Assert.NotEmpty(ccItems);

        var targetItem = ccItems.FirstOrDefault(s => s.IncidentCount >= 3);
        Assert.NotNull(targetItem);
        Assert.False(string.IsNullOrWhiteSpace(targetItem.SituationNumber));
        Assert.False(string.IsNullOrWhiteSpace(targetItem.Title));

        // 4. Query Map items
        var mapResp = await _client.GetAsync("/api/situations/open/map");
        await mapResp.EnsureSuccessWithBodyAsync();
        var mapItems = await mapResp.Content.ReadFromJsonAsync<List<SituationMapItem>>(JsonOptions);
        Assert.NotNull(mapItems);
        Assert.NotEmpty(mapItems);

        var targetMapItem = mapItems.FirstOrDefault(s => s.Id == targetItem.Id);
        Assert.NotNull(targetMapItem);
        Assert.Equal(targetItem.SituationNumber, targetMapItem.SituationNumber);
        Assert.True(targetMapItem.CenterLatitude != 0);
        Assert.True(targetMapItem.CenterLongitude != 0);
        Assert.True(targetMapItem.RadiusMeters >= 500); // Minimum 500m
        Assert.NotEmpty(targetMapItem.Incidents);

        // 5. Acknowledge situation
        var ackResp = await _client.PostAsJsonAsync($"/api/situations/{targetItem.Id}/acknowledge", new SituationActionRequest
        {
            Notes = "Acknowledged by operational coordinator."
        });
        Assert.Equal(HttpStatusCode.NoContent, ackResp.StatusCode);

        // Verify status changed to Acknowledged
        var getByIdResp = await _client.GetAsync($"/api/situations/{targetItem.Id}");
        await getByIdResp.EnsureSuccessWithBodyAsync();
        var detail = await getByIdResp.Content.ReadFromJsonAsync<OperationalSituationResponse>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(EscalationStatus.Acknowledged, detail.Status);

        // 6. Non-destructive safety check: verify member incidents remain active
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        foreach (var inc in createdIncidents)
        {
            var dbInc = await db.Incidents.AsNoTracking().FirstOrDefaultAsync(i => i.Id == inc.Id);
            Assert.NotNull(dbInc);
            Assert.NotEqual(IncidentStatus.Closed, dbInc.Status);
            Assert.NotEqual(IncidentStatus.Rejected, dbInc.Status);
        }
    }
}
