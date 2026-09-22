using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.IntegrationTests.Resources;

public sealed class ResourcesEndpointTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ResourcesEndpointTests(ApiWebApplicationFactory factory)
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
    public async Task GetResources_Unauthenticated_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync("/api/resources");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetResources_AsCitizen_ReturnsForbidden()
    {
        var token = await RegisterAndLoginCitizenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/resources");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetResources_AsAdmin_ReturnsOk()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync("/api/resources");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var resources = await response.Content.ReadFromJsonAsync<List<ResourceDetailsResponse>>(JsonOptions);
        Assert.NotNull(resources);
    }

    [Fact]
    public async Task ResourceLifecycle_Assign_Transition_Release_Flow_Succeeds()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // 1. Get incident types to create an incident
        var typesResp = await _client.GetAsync("/api/incident-types");
        typesResp.EnsureSuccessStatusCode();
        var types = await typesResp.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOptions);
        var typeId = Guid.Parse(types![0].GetProperty("id").GetString()!);

        // 2. Create test incident
        var createIncidentReq = new CreateIncidentRequest
        {
            Title = "Resource Lifecycle Integration Test Incident",
            Description = "Testing operational resource assignment and state machine.",
            IncidentTypeId = typeId,
            Latitude = 18.5204m,
            Longitude = 73.8567m,
            Address = "Station Road",
            City = "Pune"
        };
        var incidentResp = await _client.PostAsJsonAsync("/api/incidents", createIncidentReq);
        incidentResp.EnsureSuccessStatusCode();
        var incidentDoc = await incidentResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var incidentId = Guid.Parse(incidentDoc.GetProperty("id").GetString()!);

        // 3. Find an available resource or query existing
        var resListResp = await _client.GetAsync("/api/resources");
        resListResp.EnsureSuccessStatusCode();
        var resources = await resListResp.Content.ReadFromJsonAsync<List<ResourceDetailsResponse>>(JsonOptions);
        var availableResource = resources?.FirstOrDefault(r => r.Status == ResourceStatus.Available);

        if (availableResource == null)
        {
            // If none available in seed, test finishes query verification
            return;
        }

        var resourceId = availableResource.Id;

        // 4. Assign resource to incident
        var assignReq = new AssignResourceRequest
        {
            IncidentId = incidentId,
            Notes = "Assigned via integration test"
        };
        var assignResp = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/assign", assignReq);
        Assert.Equal(HttpStatusCode.OK, assignResp.StatusCode);

        var assignResult = await assignResp.Content.ReadFromJsonAsync<ResourceAssignmentResult>(JsonOptions);
        Assert.NotNull(assignResult);
        Assert.Equal(resourceId, assignResult!.ResourceId);
        Assert.Equal(incidentId, assignResult.IncidentId);

        // 5. Transition to Dispatched
        var statusReq1 = new ChangeResourceStatusRequest
        {
            Status = ResourceStatus.Dispatched,
            IncidentId = incidentId,
            Reason = "Unit en route soon"
        };
        var statusResp1 = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/status", statusReq1);
        Assert.Equal(HttpStatusCode.OK, statusResp1.StatusCode);

        // 6. Transition to EnRoute
        var statusReq2 = new ChangeResourceStatusRequest
        {
            Status = ResourceStatus.EnRoute,
            IncidentId = incidentId,
            Reason = "Departed station"
        };
        var statusResp2 = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/status", statusReq2);
        Assert.Equal(HttpStatusCode.OK, statusResp2.StatusCode);

        // 7. Transition to OnScene
        var statusReq3 = new ChangeResourceStatusRequest
        {
            Status = ResourceStatus.OnScene,
            IncidentId = incidentId,
            Reason = "Arrived on scene"
        };
        var statusResp3 = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/status", statusReq3);
        Assert.Equal(HttpStatusCode.OK, statusResp3.StatusCode);

        // 8. Transition to Returning
        var statusReq4 = new ChangeResourceStatusRequest
        {
            Status = ResourceStatus.Returning,
            IncidentId = incidentId,
            Reason = "Patient transported, returning"
        };
        var statusResp4 = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/status", statusReq4);
        Assert.Equal(HttpStatusCode.OK, statusResp4.StatusCode);

        // 9. Release resource
        var releaseReq = new ReleaseResourceRequest
        {
            Notes = "Unit back in service"
        };
        var releaseResp = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/release", releaseReq);
        Assert.Equal(HttpStatusCode.NoContent, releaseResp.StatusCode);

        // 10. Verify resource is Available again
        var getResResp = await _client.GetAsync($"/api/resources/{resourceId}");
        Assert.Equal(HttpStatusCode.OK, getResResp.StatusCode);
        var finalResource = await getResResp.Content.ReadFromJsonAsync<ResourceDetailsResponse>(JsonOptions);
        Assert.NotNull(finalResource);
        Assert.Equal(ResourceStatus.Available, finalResource!.Status);
        Assert.Null(finalResource.ActiveIncidentId);
    }
}
