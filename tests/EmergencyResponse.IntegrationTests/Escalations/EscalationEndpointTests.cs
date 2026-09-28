using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Escalation.DTOs;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EmergencyResponse.IntegrationTests.Escalations;

public sealed class EscalationEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EscalationEndpointTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
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

    private async Task<Guid> GetFirstIncidentTypeIdAsync(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await _client.GetAsync("/api/incident-types");
        resp.EnsureSuccessStatusCode();

        var types = await resp.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOptions);
        return Guid.Parse(types![0].GetProperty("id").GetString()!);
    }

    [Fact]
    public async Task GetOpenEscalations_Unauthenticated_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/escalations/open");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenEscalations_CitizenRole_ReturnsForbidden()
    {
        var token = await RegisterAndLoginCitizenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/escalations/open");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Escalation_FullLifecycle_AcknowledgeAndResolve_Succeeds()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var incidentTypeId = await GetFirstIncidentTypeIdAsync(adminToken);

        // 1. Create an incident via API
        var createResp = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest
        {
            Title = "High Rise Structural Fire",
            Description = "Multiple floors on fire with reports of trapped civilians.",
            IncidentTypeId = incidentTypeId,
            Latitude = 51.5074m,
            Longitude = -0.1278m,
            Address = "123 High Street",
            City = "London"
        });
        createResp.EnsureSuccessStatusCode();
        var incident = await createResp.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);
        Assert.NotNull(incident);

        // 2. Seed an open escalation for this incident directly via DB context
        Guid escalationId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var escalation = new IncidentEscalation
            {
                IncidentId = incident.Id,
                Type = EscalationType.PeopleInDanger,
                Level = EscalationLevel.Critical,
                Status = EscalationStatus.Open,
                Reason = "People appear to be trapped on floor 4.",
                EvidenceJson = "[\"PeopleTrapped = true\"]",
                Score = 1.0,
                RequiresHumanReview = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.IncidentEscalations.Add(escalation);
            await db.SaveChangesAsync();
            escalationId = escalation.Id;
        }

        // 3. Fetch open escalations for incident
        var listResp = await _client.GetAsync($"/api/escalations/incident/{incident.Id}");
        listResp.EnsureSuccessStatusCode();
        var escalations = await listResp.Content.ReadFromJsonAsync<List<IncidentEscalationResponse>>(JsonOptions);
        Assert.NotNull(escalations);
        var openEsc = Assert.Single(escalations, e => e.Id == escalationId);
        Assert.Equal(EscalationStatus.Open, openEsc.Status);
        Assert.Equal(EscalationType.PeopleInDanger, openEsc.Type);

        // 4. Acknowledge the escalation
        var ackResp = await _client.PostAsJsonAsync($"/api/escalations/{openEsc.Id}/acknowledge",
            new EscalationActionRequest { Notes = "Command unit notified and acknowledging." });
        Assert.Equal(HttpStatusCode.NoContent, ackResp.StatusCode);

        // Fetch to verify acknowledged state
        var afterAckResp = await _client.GetAsync($"/api/escalations/incident/{incident.Id}");
        afterAckResp.EnsureSuccessStatusCode();
        var afterAckList = await afterAckResp.Content.ReadFromJsonAsync<List<IncidentEscalationResponse>>(JsonOptions);
        var ackEsc = Assert.Single(afterAckList!, e => e.Id == openEsc.Id);
        Assert.Equal(EscalationStatus.Acknowledged, ackEsc.Status);
        Assert.NotNull(ackEsc.AcknowledgedAtUtc);

        // 5. Resolve the escalation
        var resolveResp = await _client.PostAsJsonAsync($"/api/escalations/{openEsc.Id}/resolve",
            new EscalationActionRequest { Notes = "Civilians safely evacuated." });
        Assert.Equal(HttpStatusCode.NoContent, resolveResp.StatusCode);

        // Fetch to verify resolved state
        var afterResolveResp = await _client.GetAsync($"/api/escalations/incident/{incident.Id}");
        afterResolveResp.EnsureSuccessStatusCode();
        var afterResolveList = await afterResolveResp.Content.ReadFromJsonAsync<List<IncidentEscalationResponse>>(JsonOptions);
        var resolvedEsc = Assert.Single(afterResolveList!, e => e.Id == openEsc.Id);
        Assert.Equal(EscalationStatus.Resolved, resolvedEsc.Status);
        Assert.NotNull(resolvedEsc.ResolvedAtUtc);
        Assert.Equal("Civilians safely evacuated.", resolvedEsc.ResolutionNotes);

        // 6. Attempt to acknowledge an already resolved (terminal) escalation -> should fail
        var reAckResp = await _client.PostAsJsonAsync($"/api/escalations/{openEsc.Id}/acknowledge",
            new EscalationActionRequest { Notes = "Attempt re-acknowledge" });
        Assert.Equal(HttpStatusCode.BadRequest, reAckResp.StatusCode);
    }

    [Fact]
    public async Task EvaluateIncident_Deduplication_DoesNotDuplicateOpenEscalations()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var incidentTypeId = await GetFirstIncidentTypeIdAsync(adminToken);

        // 1. Create an incident
        var createResp = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest
        {
            Title = "Flooding in subway tunnel",
            Description = "Subway station rapidly flooding.",
            IncidentTypeId = incidentTypeId,
            Latitude = 51.5074m,
            Longitude = -0.1278m,
            Address = "Station Square",
            City = "London"
        });
        createResp.EnsureSuccessStatusCode();
        var incident = await createResp.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);
        Assert.NotNull(incident);

        // 2. Seed an initial open escalation
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var escalation = new IncidentEscalation
            {
                IncidentId = incident.Id,
                Type = EscalationType.ImmediateDanger,
                Level = EscalationLevel.Critical,
                Status = EscalationStatus.Open,
                Reason = "Flooding water rising rapidly.",
                EvidenceJson = "[\"ImmediateDanger = true\"]",
                Score = 1.0,
                RequiresHumanReview = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            db.IncidentEscalations.Add(escalation);
            await db.SaveChangesAsync();
        }

        // 3. Trigger evaluation endpoint
        var evalResp = await _client.PostAsync($"/api/escalations/evaluate/{incident.Id}", null);
        evalResp.EnsureSuccessStatusCode();

        // 4. Verify that deduplication kept only 1 open escalation of this type
        var listResp = await _client.GetAsync($"/api/escalations/incident/{incident.Id}");
        listResp.EnsureSuccessStatusCode();
        var escalations = await listResp.Content.ReadFromJsonAsync<List<IncidentEscalationResponse>>(JsonOptions);
        Assert.NotNull(escalations);

        var immediateDangerAlerts = escalations.Where(e => e.Type == EscalationType.ImmediateDanger && e.Status == EscalationStatus.Open);
        Assert.Single(immediateDangerAlerts);
    }
}
