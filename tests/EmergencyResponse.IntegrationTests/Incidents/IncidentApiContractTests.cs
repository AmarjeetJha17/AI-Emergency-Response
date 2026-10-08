using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.IntegrationTests.Infrastructure;

namespace EmergencyResponse.IntegrationTests.Incidents;

[Collection(IntegrationTestCollection.Name)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
public sealed class IncidentApiContractTests : IntegrationTestBase
{
    public IncidentApiContractTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task IncidentList_ReturnsStablePaginationContract()
    {
        AuthenticateAs(TestUsers.OperatorId.ToString(), ApplicationRoles.Operator);

        var response = await Client.GetAsync("/api/incidents?page=1&pageSize=20");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.Equal(1, payload.GetProperty("page").GetInt32());
        Assert.Equal(20, payload.GetProperty("pageSize").GetInt32());
        Assert.True(payload.GetProperty("totalCount").GetInt32() >= 0);
        Assert.True(payload.GetProperty("totalPages").GetInt32() >= 0);
    }

    [Fact]
    public async Task IncidentList_WithoutAuthentication_ReturnsUnauthorized()
    {
        var response = await Client.GetAsync("/api/incidents?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateIncident_WithMissingRequiredFields_ReturnsValidationProblem()
    {
        AuthenticateAs(TestUsers.CitizenId.ToString(), ApplicationRoles.Citizen);

        var response = await Client.PostAsJsonAsync("/api/incidents", new { title = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }
}
