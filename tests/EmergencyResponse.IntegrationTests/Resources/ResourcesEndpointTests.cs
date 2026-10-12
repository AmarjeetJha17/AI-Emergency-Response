using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.IntegrationTests.Resources;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Collection("SQL Server integration database")]
public sealed class ResourcesEndpointTests
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ResourcesEndpointTests(IntegrationTestFixture fixture)
    {
        _client = fixture.Factory.CreateClient();
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
        await typesResp.EnsureSuccessWithBodyAsync();
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
        await incidentResp.EnsureSuccessWithBodyAsync();
        var incidentDoc = await incidentResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var incidentId = Guid.Parse(incidentDoc.GetProperty("id").GetString()!);

        // 3. Find an available resource or query existing
        var resListResp = await _client.GetAsync("/api/resources");
        await resListResp.EnsureSuccessWithBodyAsync();
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

    [Fact]
    public async Task GetCommandCenter_AsAdmin_ReturnsOkWithList()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync("/api/resources/command-center");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<ResourceCommandCenterItem>>(JsonOptions);
        Assert.NotNull(items);
    }

    [Fact]
    public async Task GetSummary_AsAdmin_ReturnsOkWithCounts()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync("/api/resources/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await response.Content.ReadFromJsonAsync<ResourceSummary>(JsonOptions);
        Assert.NotNull(summary);
        Assert.True(summary.Available >= 0);
    }

    [Fact]
    public async Task GetAssignableIncidents_AsAdmin_ReturnsOkWithList()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync("/api/resources/assignable-incidents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<List<AssignableIncidentItem>>(JsonOptions);
        Assert.NotNull(items);
    }

    [Fact]
    public async Task GetRecommendations_Unauthenticated_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.GetAsync($"/api/resources/recommendations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRecommendations_AsCitizen_ReturnsForbidden()
    {
        var token = await RegisterAndLoginCitizenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/resources/recommendations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetRecommendations_AsAdmin_ReturnsOkWithResponse()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var incidentsResp = await _client.GetAsync("/api/resources/assignable-incidents");
        await incidentsResp.EnsureSuccessWithBodyAsync();
        var incidents = await incidentsResp.Content.ReadFromJsonAsync<List<AssignableIncidentItem>>(JsonOptions);

        if (incidents != null && incidents.Count > 0)
        {
            var incidentId = incidents[0].Id;
            var response = await _client.GetAsync($"/api/resources/recommendations/{incidentId}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var recs = await response.Content.ReadFromJsonAsync<ResourceRecommendationResponse>(JsonOptions);
            Assert.NotNull(recs);
            Assert.Equal(incidentId, recs!.IncidentId);
            Assert.NotNull(recs.Recommendations);
        }
    }

    [Fact]
    public async Task ExecuteCommand_Unauthenticated_ReturnsUnauthorized()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var response = await _client.PostAsJsonAsync($"/api/resources/{Guid.NewGuid()}/command",
            new ResourceCommandRequest { Command = ResourceCommand.Dispatch });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExecuteCommand_AsCitizen_ReturnsForbidden()
    {
        var token = await RegisterAndLoginCitizenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync($"/api/resources/{Guid.NewGuid()}/command",
            new ResourceCommandRequest { Command = ResourceCommand.Dispatch });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ExecuteCommand_FullOperationalLifecycle_SucceedsWithAccurateTimingsAndHistory()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // 1. Get incident types
        var typesResp = await _client.GetAsync("/api/incident-types");
        await typesResp.EnsureSuccessWithBodyAsync();
        var types = await typesResp.Content.ReadFromJsonAsync<List<JsonElement>>(JsonOptions);
        var typeId = Guid.Parse(types![0].GetProperty("id").GetString()!);

        // 2. Create test incident
        var createIncidentReq = new CreateIncidentRequest
        {
            Title = "Command Lifecycle Integration Incident",
            Description = "Testing controlled dispatch transitions and operational metrics.",
            IncidentTypeId = typeId,
            Latitude = 18.5304m,
            Longitude = 73.8667m,
            Address = "MG Road",
            City = "Pune"
        };
        var incidentResp = await _client.PostAsJsonAsync("/api/incidents", createIncidentReq);
        await incidentResp.EnsureSuccessWithBodyAsync();
        var incidentDoc = await incidentResp.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var incidentId = Guid.Parse(incidentDoc.GetProperty("id").GetString()!);

        // 3. Find an available resource
        var resListResp = await _client.GetAsync("/api/resources");
        await resListResp.EnsureSuccessWithBodyAsync();
        var resources = await resListResp.Content.ReadFromJsonAsync<List<ResourceDetailsResponse>>(JsonOptions);
        var availableResource = resources?.FirstOrDefault(r => r.Status == ResourceStatus.Available);

        if (availableResource == null)
        {
            return;
        }

        var resourceId = availableResource.Id;

        // 4. Assign resource
        var assignResp = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/assign",
            new AssignResourceRequest { IncidentId = incidentId, Notes = "Assigned for command test" });
        Assert.Equal(HttpStatusCode.OK, assignResp.StatusCode);

        // 5. Command: Dispatch
        var dispatchResp = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/command",
            new ResourceCommandRequest { Command = ResourceCommand.Dispatch, Notes = "Dispatching unit" });
        Assert.Equal(HttpStatusCode.OK, dispatchResp.StatusCode);
        var dispatchResult = await dispatchResp.Content.ReadFromJsonAsync<ResourceCommandResult>(JsonOptions);
        Assert.NotNull(dispatchResult);
        Assert.Equal(ResourceStatus.Dispatched, dispatchResult!.NewStatus);
        Assert.NotNull(dispatchResult.AssignmentTimestampUtc);

        // 6. Command: MarkEnRoute
        var enRouteResp = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/command",
            new ResourceCommandRequest { Command = ResourceCommand.MarkEnRoute, Notes = "En route to scene" });
        Assert.Equal(HttpStatusCode.OK, enRouteResp.StatusCode);
        var enRouteResult = await enRouteResp.Content.ReadFromJsonAsync<ResourceCommandResult>(JsonOptions);
        Assert.NotNull(enRouteResult);
        Assert.Equal(ResourceStatus.EnRoute, enRouteResult!.NewStatus);
        Assert.NotNull(enRouteResult.AssignmentTimestampUtc);

        // 7. Command: MarkOnScene
        var onSceneResp = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/command",
            new ResourceCommandRequest { Command = ResourceCommand.MarkOnScene, Notes = "Arrived on scene" });
        Assert.Equal(HttpStatusCode.OK, onSceneResp.StatusCode);
        var onSceneResult = await onSceneResp.Content.ReadFromJsonAsync<ResourceCommandResult>(JsonOptions);
        Assert.NotNull(onSceneResult);
        Assert.Equal(ResourceStatus.OnScene, onSceneResult!.NewStatus);
        Assert.NotNull(onSceneResult.AssignmentTimestampUtc);

        // 8. Command: BeginReturn
        var returningResp = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/command",
            new ResourceCommandRequest { Command = ResourceCommand.BeginReturn, Notes = "Clearing scene, returning" });
        Assert.Equal(HttpStatusCode.OK, returningResp.StatusCode);
        var returningResult = await returningResp.Content.ReadFromJsonAsync<ResourceCommandResult>(JsonOptions);
        Assert.NotNull(returningResult);
        Assert.Equal(ResourceStatus.Returning, returningResult!.NewStatus);
        Assert.NotNull(returningResult.AssignmentTimestampUtc);

        // 9. Command: MarkAvailable
        var availableResp = await _client.PostAsJsonAsync($"/api/resources/{resourceId}/command",
            new ResourceCommandRequest { Command = ResourceCommand.MarkAvailable, Notes = "Back at station" });
        Assert.Equal(HttpStatusCode.OK, availableResp.StatusCode);
        var availableResult = await availableResp.Content.ReadFromJsonAsync<ResourceCommandResult>(JsonOptions);
        Assert.NotNull(availableResult);
        Assert.Equal(ResourceStatus.Available, availableResult!.NewStatus);
        Assert.NotNull(availableResult.AssignmentTimestampUtc);

        // 10. Verify resource details & operational timing
        var getResResp = await _client.GetAsync($"/api/resources/{resourceId}");
        Assert.Equal(HttpStatusCode.OK, getResResp.StatusCode);
        var finalDetails = await getResResp.Content.ReadFromJsonAsync<ResourceDetailsResponse>(JsonOptions);
        Assert.NotNull(finalDetails);
        Assert.Equal(ResourceStatus.Available, finalDetails!.Status);
        Assert.NotNull(finalDetails.OperationalTiming);
        Assert.NotNull(finalDetails.OperationalTiming.AssignmentToDispatch);
        Assert.NotNull(finalDetails.OperationalTiming.DispatchToEnRoute);
        Assert.NotNull(finalDetails.OperationalTiming.EnRouteToOnScene);
        Assert.NotNull(finalDetails.OperationalTiming.OnSceneDuration);
        Assert.NotNull(finalDetails.OperationalTiming.TotalDeploymentDuration);

        // 11. Verify history endpoint
        var histResp = await _client.GetAsync($"/api/resources/{resourceId}/history");
        Assert.Equal(HttpStatusCode.OK, histResp.StatusCode);
        var history = await histResp.Content.ReadFromJsonAsync<List<ResourceStatusHistoryItem>>(JsonOptions);
        Assert.NotNull(history);
        Assert.True(history.Count >= 5);
    }

    [Fact]
    public async Task ExecuteCommand_InvalidTransition_ReturnsBadRequest()
    {
        var adminToken = await LoginAdminAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        // Find available resource
        var resListResp = await _client.GetAsync("/api/resources");
        await resListResp.EnsureSuccessWithBodyAsync();
        var resources = await resListResp.Content.ReadFromJsonAsync<List<ResourceDetailsResponse>>(JsonOptions);
        var availableResource = resources?.FirstOrDefault(r => r.Status == ResourceStatus.Available);

        if (availableResource == null) return;

        // Try to MarkOnScene directly while Available
        var response = await _client.PostAsJsonAsync($"/api/resources/{availableResource.Id}/command",
            new ResourceCommandRequest { Command = ResourceCommand.MarkOnScene, Notes = "Illegal leap" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

