using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.IntegrationTests.Incidents;

[Collection(IntegrationTestCollection.Name)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
public sealed class IncidentCreationTests : IntegrationTestBase
{
    public IncidentCreationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CitizenCreation_PersistsAuthoritativeFields_AndIgnoresClientOverrides()
    {
        AuthenticateAs(TestUsers.CitizenId.ToString(), ApplicationRoles.Citizen);
        var typeId = await ExecuteDbAsync(db => db.IncidentTypes
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .FirstAsync());
        var title = $"Integration incident {Guid.NewGuid():N}";

        var response = await Client.PostAsJsonAsync("/api/incidents", new
        {
            title,
            description = "Flood water has blocked the road near the test location.",
            incidentTypeId = typeId,
            latitude = 18.5204m,
            longitude = 73.8567m,
            address = "Test Road",
            city = "Test City",
            peopleAffected = 2,
            vehiclesAffected = 1,
            casualties = 0,
            incidentNumber = "INC-HACKED",
            status = "Resolved",
            severity = "Critical",
            priority = "Critical",
            reporterUserId = TestUsers.OtherOperatorId.ToString()
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var incidentId = body.GetProperty("id").GetGuid();
        var persisted = await ExecuteDbAsync(db => db.Incidents
            .AsNoTracking()
            .SingleAsync(x => x.Id == incidentId));

        Assert.Equal(title, persisted.Title);
        Assert.Equal(TestUsers.CitizenId.ToString(), persisted.ReporterUserId);
        Assert.NotEqual("INC-HACKED", persisted.IncidentNumber);
        Assert.NotEqual(EmergencyResponse.Domain.Enums.IncidentStatus.Resolved, persisted.Status);
        Assert.NotEqual(EmergencyResponse.Domain.Enums.SeverityLevel.Critical, persisted.Severity);
        Assert.NotEqual(EmergencyResponse.Domain.Enums.PriorityLevel.Critical, persisted.Priority);
    }

    [Fact]
    public async Task Citizen_CannotReadAnotherCitizensIncident()
    {
        AuthenticateAs(TestUsers.CitizenId.ToString(), ApplicationRoles.Citizen);
        var typeId = await ExecuteDbAsync(db => db.IncidentTypes
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .FirstAsync());

        var createResponse = await Client.PostAsJsonAsync("/api/incidents", new
        {
            title = $"Private incident {Guid.NewGuid():N}",
            description = "Sensitive incident report for ownership access test.",
            incidentTypeId = typeId,
            latitude = 18.5204m,
            longitude = 73.8567m,
            address = "Test Road",
            city = "Test City",
            peopleAffected = 1,
            vehiclesAffected = 0,
            casualties = 0
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var body = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var incidentId = body.GetProperty("id").GetGuid();

        AuthenticateAs(TestUsers.OtherOperatorId.ToString(), ApplicationRoles.Citizen);
        var readResponse = await Client.GetAsync($"/api/incidents/{incidentId}");

        Assert.Contains(readResponse.StatusCode, new[]
        {
            HttpStatusCode.Forbidden,
            HttpStatusCode.NotFound
        });
    }
}
