using System.Net;
using System.Net.Http.Json;
using EmergencyResponse.Application.DTOs.Incidents;
using EmergencyResponse.Application.Intelligence;
using EmergencyResponse.Application.Situations.DTOs;
using EmergencyResponse.Infrastructure.Identity;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.IntegrationTests.Portfolio;

[Collection(IntegrationTestCollection.Name)]
public sealed class PortfolioDemoSeederIntegrationTests(IntegrationTestFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task Seed_IsVisibleThroughAuthorizedOperationalApis_AndCleanupIsScoped()
    {
        var reporterId = Guid.NewGuid().ToString();
        var reviewerId = await ExecuteDbAsync(async db =>
        {
            var citizenRole = await db.Roles.SingleAsync(x => x.Name == "Citizen");
            db.Users.Add(new ApplicationUser
            {
                Id = reporterId, UserName = $"DEMO18-{reporterId}@test.local", NormalizedUserName = $"DEMO18-{reporterId}@test.local".ToUpperInvariant(),
                Email = $"DEMO18-{reporterId}@test.local", NormalizedEmail = $"DEMO18-{reporterId}@test.local".ToUpperInvariant(),
                FirstName = "DEMO18", LastName = "Citizen", IsActive = true
            });
            db.UserRoles.Add(new IdentityUserRole<string> { UserId = reporterId, RoleId = citizenRole.Id });
            await db.SaveChangesAsync();
            return (await db.Users.SingleAsync(x => x.Email == "admin@test.local")).Id;
        });
        var options = new PortfolioDemoSeedOptions("Test", reporterId, reviewerId);
        try
        {
            var seeded = await ExecuteDbAsync(db => DatabaseSeeder.SeedPortfolioDemoAsync(db, options));
            var again = await ExecuteDbAsync(db => DatabaseSeeder.SeedPortfolioDemoAsync(db, options));
            Assert.True(seeded.Created);
            Assert.False(again.Created);
            Assert.Equal(seeded.IncidentIds, again.IncidentIds);

            AuthenticateAs(reviewerId, "SystemAdmin");
            foreach (var id in seeded.IncidentIds)
            {
                var response = await Client.GetAsync($"/api/incidents/{id}");
                await response.EnsureSuccessWithBodyAsync();
                var incident = await response.Content.ReadFromJsonAsync<IncidentResponse>();
                Assert.NotNull(incident);
                Assert.StartsWith("DEMO18-", incident.IncidentNumber);
                Assert.Null(incident.Casualties);
                Assert.False(incident.HasDuplicateCandidates);
                Assert.Empty(incident.VisualEvidence);
                Assert.Single(incident.StatusHistory);
                using var contextResponse = await Client.GetAsync($"/api/incidents/{id}/context");
                await contextResponse.EnsureSuccessWithBodyAsync();
                var context = await contextResponse.Content.ReadFromJsonAsync<IncidentContextSnapshot>();
                Assert.NotNull(context);
                Assert.Equal(incident.PeopleAffected, context.Incident.PeopleAffected);
                Assert.Equal(incident.VehiclesAffected, context.Incident.VehiclesAffected);
                Assert.Null(context.Incident.Casualties);
                using var duplicates = await Client.GetAsync($"/api/incidents/{id}/duplicates");
                Assert.Equal(HttpStatusCode.Conflict, duplicates.StatusCode);
                Assert.Contains("No duplicate conclusion", await duplicates.Content.ReadAsStringAsync());
                Assert.False(await ExecuteDbAsync(db => db.IncidentDuplicateMatches.AnyAsync(
                    x => x.SourceIncidentId == id || x.CandidateIncidentId == id)));
            }
            using var situationResponse = await Client.GetAsync($"/api/situations/{seeded.SituationIds[0]}");
            await situationResponse.EnsureSuccessWithBodyAsync();
            var situation = await situationResponse.Content.ReadFromJsonAsync<OperationalSituationResponse>();
            Assert.NotNull(situation);
            Assert.Equal(3, situation.Incidents.Count);
            Assert.All(situation.Incidents, item => Assert.Null(item.Casualties));
            foreach (var (route, marker) in new[]
            {
                ("/api/resources/command-center", "DEMO18-BOAT"),
                ("/api/situations/command-center", "DEMO18-SITUATION-FLOOD"),
                ("/api/escalations/open", "DEMO18 manual exercise escalation"),
                ("/api/notifications", "DEMO18 seeded manual review reminder"),
                ("/api/map/incidents", "DEMO18-FLOOD-01")
            })
            {
                var response = await Client.GetAsync(route);
                await response.EnsureSuccessWithBodyAsync();
                Assert.Contains(marker, await response.Content.ReadAsStringAsync());
            }

            AuthenticateAs(reporterId, "Citizen");
            var owned = await Client.GetAsync($"/api/incidents/{seeded.IncidentIds[0]}");
            Assert.Equal(HttpStatusCode.OK, owned.StatusCode);
            var inbox = await Client.GetAsync("/api/notifications");
            await inbox.EnsureSuccessWithBodyAsync();
            Assert.DoesNotContain("DEMO18-", await inbox.Content.ReadAsStringAsync());
            var forbidden = await Client.GetAsync("/api/resources/command-center");
            Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
            AuthenticateAs(Guid.NewGuid().ToString(), "Citizen");
            var otherCitizen = await Client.GetAsync($"/api/incidents/{seeded.IncidentIds[0]}");
            Assert.Contains(otherCitizen.StatusCode, new[] { HttpStatusCode.NotFound, HttpStatusCode.Forbidden });

            var before = await ExecuteDbAsync(db => db.Incidents.CountAsync());
            var dryRun = await ExecuteDbAsync(db => DatabaseSeeder.CleanupPortfolioDemoAsync(db, "Test"));
            Assert.False(dryRun.Applied);
            Assert.Equal(6, dryRun.RecordCounts["Incident"]);
            Assert.Equal(before, await ExecuteDbAsync(db => db.Incidents.CountAsync()));
            await ExecuteDbAsync(db => DatabaseSeeder.CleanupPortfolioDemoAsync(db, "Test", apply: true));
            Assert.Equal(before - 6, await ExecuteDbAsync(db => db.Incidents.CountAsync()));
            Assert.True(await ExecuteDbAsync(db => db.Users.AnyAsync(x => x.Id == reporterId)));
            Assert.True(await ExecuteDbAsync(db => db.Departments.AnyAsync()));
        }
        finally
        {
            await ExecuteDbAsync(db => DatabaseSeeder.CleanupPortfolioDemoAsync(db, "Test", apply: true));
            // Only this test owns this temporary account; the demo importer does not.
            await ExecuteDbAsync(async db =>
            {
                var actor = await db.Users.FindAsync(reporterId);
                if (actor is not null) { db.Users.Remove(actor); await db.SaveChangesAsync(); }
            });
        }
    }
}
