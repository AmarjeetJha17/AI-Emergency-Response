using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.IntegrationTests.Smoke;

[Collection(IntegrationTestCollection.Name)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Smoke)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.EndToEnd)]
public sealed class IncidentOperationalLifecycleTests : IntegrationTestBase
{
    public IncidentOperationalLifecycleTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Fact]
    public async Task CitizenIncident_IsPersisted_AndOperatorCanAssignAndDispatchResource()
    {
        var departmentId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        await ExecuteDbAsync(async db =>
        {
            db.Departments.Add(new Department
            {
                Id = departmentId,
                Name = "Lifecycle test department",
                Code = $"LC-{departmentId:N}"[..12],
                CreatedAtUtc = DateTime.UtcNow
            });
            db.Resources.Add(new Resource
            {
                Id = resourceId,
                Name = "Lifecycle test ambulance",
                Type = ResourceType.Ambulance,
                Status = ResourceStatus.Available,
                DepartmentId = departmentId,
                CreatedAtUtc = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        });

        AuthenticateAs(TestUsers.CitizenId.ToString(), ApplicationRoles.Citizen);
        var incidentTypeId = await ExecuteDbAsync(db => db.IncidentTypes
            .Where(x => x.IsActive).Select(x => x.Id).FirstAsync());
        var create = await Client.PostAsJsonAsync("/api/incidents", new
        {
            title = $"Lifecycle incident {Guid.NewGuid():N}",
            description = "Synthetic incident used to verify the operational lifecycle.",
            incidentTypeId,
            latitude = 18.5204m,
            longitude = 73.8567m,
            address = "Test Road",
            city = "Test City",
            peopleAffected = 2,
            vehiclesAffected = 0,
            casualties = 0
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<JsonElement>();
        var incidentId = created.GetProperty("id").GetGuid();

        var persisted = await ExecuteDbAsync(db => db.Incidents.AsNoTracking()
            .AnyAsync(x => x.Id == incidentId));
        Assert.True(persisted);
        var queued = await ExecuteDbAsync(db => db.OutboxMessages.AsNoTracking()
            .AnyAsync(x => x.PayloadJson.Contains(incidentId.ToString())));
        Assert.True(queued, "Incident creation must persist its background work in the outbox.");

        AuthenticateAs(TestUsers.OperatorId.ToString(), ApplicationRoles.Operator);
        var getIncident = await Client.GetAsync($"/api/incidents/{incidentId}");
        Assert.Equal(HttpStatusCode.OK, getIncident.StatusCode);

        AuthenticateAs(TestUsers.EmergencyAdminId.ToString(), ApplicationRoles.EmergencyAdmin);
        var assigned = await Client.PostAsJsonAsync($"/api/resources/{resourceId}/assign",
            new AssignResourceRequest { IncidentId = incidentId, Notes = "Lifecycle test assignment" });
        Assert.Equal(HttpStatusCode.OK, assigned.StatusCode);

        var dispatched = await Client.PostAsJsonAsync($"/api/resources/{resourceId}/command",
            new ResourceCommandRequest { Command = ResourceCommand.Dispatch, Notes = "Dispatch lifecycle test unit" });
        Assert.Equal(HttpStatusCode.OK, dispatched.StatusCode);
        var status = await ExecuteDbAsync(db => db.Resources.AsNoTracking()
            .Where(x => x.Id == resourceId).Select(x => x.Status).SingleAsync());
        Assert.Equal(ResourceStatus.Dispatched, status);
    }
}
