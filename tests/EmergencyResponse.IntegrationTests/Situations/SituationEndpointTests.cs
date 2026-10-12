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
public sealed class SituationEndpointTests
{
    private readonly EmergencyResponseWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SituationEndpointTests(IntegrationTestFixture fixture)
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
    public async Task GetOpenSituations_Unauthenticated_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/situations/open");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOpenSituations_CitizenRole_ReturnsForbidden()
    {
        var token = await RegisterAndLoginCitizenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/situations/open");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Situation_FullLifecycle_Evaluate_Review_Resolve_Succeeds()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var incidentTypeId = await GetFirstIncidentTypeIdAsync(adminToken);

        // 1. Create 3 clustered active incidents in close proximity
        var createdIncidents = new List<IncidentResponse>();
        var coords = new[] { (19.0760m, 72.8777m), (19.0780m, 72.8790m), (19.0800m, 72.8810m) };

        for (var i = 0; i < 3; i++)
        {
            var createResp = await _client.PostAsJsonAsync("/api/incidents", new CreateIncidentRequest
            {
                Title = $"Cluster Incident {i + 1}",
                Description = $"Severe flooding reported in sector {i + 1}.",
                IncidentTypeId = incidentTypeId,
                Latitude = coords[i].Item1,
                Longitude = coords[i].Item2,
                Address = $"Sector {i + 1}",
                City = "Mumbai"
            });
            await createResp.EnsureSuccessWithBodyAsync();
            var inc = await createResp.Content.ReadFromJsonAsync<IncidentResponse>(JsonOptions);
            Assert.NotNull(inc);
            createdIncidents.Add(inc);
        }

        // 2. Trigger evaluate
        var evalResp = await _client.PostAsync("/api/situations/evaluate", null);
        await evalResp.EnsureSuccessWithBodyAsync();

        // 3. Query open situations
        var openResp = await _client.GetAsync("/api/situations/open");
        await openResp.EnsureSuccessWithBodyAsync();
        var situations = await openResp.Content.ReadFromJsonAsync<List<OperationalSituationResponse>>(JsonOptions);
        Assert.NotNull(situations);
        Assert.NotEmpty(situations);

        var targetSituation = situations.FirstOrDefault(s => s.IncidentCount >= 3);
        Assert.NotNull(targetSituation);
        Assert.False(string.IsNullOrWhiteSpace(targetSituation.SituationNumber));
        Assert.False(string.IsNullOrWhiteSpace(targetSituation.Title));

        // 4. Query by ID
        var getByIdResp = await _client.GetAsync($"/api/situations/{targetSituation.Id}");
        await getByIdResp.EnsureSuccessWithBodyAsync();
        var detail = await getByIdResp.Content.ReadFromJsonAsync<OperationalSituationResponse>(JsonOptions);
        Assert.NotNull(detail);
        Assert.Equal(targetSituation.Id, detail.Id);
        Assert.True(detail.RequiresHumanReview);
        Assert.NotEmpty(detail.Incidents);

        // 5. Operator reviews situation
        var reviewResp = await _client.PostAsJsonAsync($"/api/situations/{targetSituation.Id}/review", new ReviewSituationRequest
        {
            Decision = "ConfirmedValid",
            Notes = "Verified cluster coordinates"
        });
        Assert.Equal(HttpStatusCode.NoContent, reviewResp.StatusCode);

        // Verify review applied
        var reviewedResp = await _client.GetAsync($"/api/situations/{targetSituation.Id}");
        var reviewedDetail = await reviewedResp.Content.ReadFromJsonAsync<OperationalSituationResponse>(JsonOptions);
        Assert.False(reviewedDetail!.RequiresHumanReview);

        // 6. Operator resolves situation
        var resolveResp = await _client.PostAsJsonAsync($"/api/situations/{targetSituation.Id}/resolve", new SituationActionRequest
        {
            Notes = "All crisis fronts secured."
        });
        Assert.Equal(HttpStatusCode.NoContent, resolveResp.StatusCode);

        // Verify situation status resolved
        var resolvedResp = await _client.GetAsync($"/api/situations/{targetSituation.Id}");
        var resolvedDetail = await resolvedResp.Content.ReadFromJsonAsync<OperationalSituationResponse>(JsonOptions);
        Assert.Equal(EscalationStatus.Resolved, resolvedDetail!.Status);

        // 7. Non-destructive safety guarantee check: Verify underlying incidents are NOT closed or altered
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
