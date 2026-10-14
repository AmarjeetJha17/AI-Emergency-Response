using System.Text.Json.Nodes;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Domain.Notifications;
using EmergencyResponse.Infrastructure.Identity;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.UnitTests.Infrastructure;

public sealed class PortfolioDemoSeederTests
{
    private static readonly PortfolioDemoSeedOptions Options = new("Test", "DEMO18-unit-citizen", "DEMO18-unit-operator");

    [Fact]
    public async Task Seed_RepeatsWithoutDuplicatesOrOverwritingReviewedState()
    {
        await using var fixture = await CreateFixtureAsync();
        var db = fixture.Context;
        var first = await DatabaseSeeder.SeedPortfolioDemoAsync(db, Options);
        Assert.True(first.Created);
        Assert.Equal(6, first.IncidentIds.Count);
        Assert.Equal(6, first.ResourceIds.Count);
        Assert.Equal(2, first.EscalationIds.Count);
        Assert.Single(first.SituationIds);
        Assert.Equal(3, first.NotificationIds.Count);
        var fire = await db.Incidents.SingleAsync(x => x.IncidentNumber == "DEMO18-FIRE-01");
        Assert.Null(fire.PeopleAffected);
        Assert.Null(fire.Casualties);
        fire.Status = IncidentStatus.UnderReview;
        var notification = await db.Notifications.FirstAsync();
        notification.IsRead = true;
        notification.Status = NotificationStatus.Read;
        notification.ReadAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        var second = await DatabaseSeeder.SeedPortfolioDemoAsync(db, Options);
        Assert.False(second.Created);
        Assert.Equal(first.IncidentIds, second.IncidentIds);
        Assert.Equal(IncidentStatus.UnderReview, fire.Status);
        Assert.True(notification.IsRead);
        Assert.Equal(3, await db.Notifications.CountAsync());
        Assert.Equal(6, await db.IncidentStatusHistory.CountAsync());
        Assert.Empty(await db.AIAnalyses.ToListAsync());
        Assert.Empty(await db.IncidentDuplicateMatches.ToListAsync());
        Assert.Empty(await db.OutboxMessages.ToListAsync());
        Assert.All(await db.Incidents.ToListAsync(), x => Assert.Equal(DuplicateDetectionStatus.NotStarted, x.DuplicateDetectionStatus));
        Assert.All(await db.Resources.ToListAsync(), x => Assert.Equal(ResourceStatus.Available, x.Status));
        var situation = await db.OperationalSituations.Include(x => x.Incidents).SingleAsync();
        Assert.Equal(situation.IncidentCount, situation.Incidents.Count);
        Assert.Equal(3, situation.Incidents.Count);
        Assert.Single(situation.Incidents, x => x.IsPrimaryIncident);
        Assert.Null(situation.LastAIAnalysisAtUtc);
        Assert.All(await db.IncidentEscalations.ToListAsync(), x =>
        {
            Assert.Equal(EscalationType.ManualEscalation, x.Type);
            Assert.Null(x.Score);
            Assert.True(x.RequiresHumanReview);
        });
        Assert.All(await db.Notifications.ToListAsync(), x => Assert.Equal(Options.OperatorUserId, x.UserId));
    }

    [Fact]
    public async Task Cleanup_DryRunAndRepeatKeepSharedAndLookalikeRows()
    {
        await using var fixture = await CreateFixtureAsync();
        var db = fixture.Context;
        var snapshot = await DatabaseSeeder.SeedPortfolioDemoAsync(db, Options);
        var typeId = await db.IncidentTypes.Select(x => x.Id).FirstAsync();
        var foreign = new Incident
        {
            Id = Guid.NewGuid(), IncidentNumber = "DEMO18-NOT-OWNED", Title = "DEMO18-user-owned lookalike",
            Description = "Not owned by the demo importer.", IncidentTypeId = typeId,
            Status = IncidentStatus.Submitted, Severity = SeverityLevel.Low, Priority = PriorityLevel.Normal,
            ReportedAtUtc = DateTime.UtcNow
        };
        db.Incidents.Add(foreign);
        await db.SaveChangesAsync();
        var plan = await DatabaseSeeder.CleanupPortfolioDemoAsync(db, "Test");
        Assert.False(plan.Applied);
        Assert.Equal(6, plan.RecordCounts[nameof(Incident)]);
        Assert.Equal(7, await db.Incidents.CountAsync());
        var cleanup = await DatabaseSeeder.CleanupPortfolioDemoAsync(db, "Test", apply: true);
        Assert.True(cleanup.Applied);
        Assert.Equal(foreign.Id, (await db.Incidents.SingleAsync()).Id);
        Assert.Equal(10, await db.IncidentTypes.CountAsync());
        Assert.Equal(6, await db.Departments.CountAsync());
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Empty(await db.Resources.ToListAsync());
        Assert.Empty(await db.Notifications.ToListAsync());
        Assert.Empty(await db.OperationalSituations.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
        var repeat = await DatabaseSeeder.CleanupPortfolioDemoAsync(db, "Test", apply: true);
        Assert.False(repeat.Applied);
        Assert.Empty(repeat.RecordCounts);
        var reseed = await DatabaseSeeder.SeedPortfolioDemoAsync(db, Options);
        Assert.True(reseed.Created);
        Assert.Equal(snapshot.IncidentIds, reseed.IncidentIds);
    }

    [Theory]
    [InlineData("note")]
    [InlineData("notification")]
    [InlineData("audit")]
    [InlineData("outbox")]
    public async Task Cleanup_RefusesForeignDependentsWithoutChangingEitherDataset(string kind)
    {
        await using var fixture = await CreateFixtureAsync();
        var db = fixture.Context;
        var seed = await DatabaseSeeder.SeedPortfolioDemoAsync(db, Options);
        var incidentId = seed.IncidentIds[0];
        if (kind == "note") db.IncidentNotes.Add(new() { Id = Guid.NewGuid(), IncidentId = incidentId, CreatedByUserId = Options.OperatorUserId, Content = "Operator-owned note" });
        if (kind == "notification") db.Notifications.Add(new() { Id = Guid.NewGuid(), IncidentId = incidentId, UserId = Options.OperatorUserId, Title = "Operator-owned reminder", Message = "Do not clear my FK." });
        if (kind == "audit") db.AuditLogs.Add(new() { Id = Guid.NewGuid(), Action = "OperatorChange", EntityName = nameof(Incident), EntityId = incidentId.ToString(), OccurredAtUtc = DateTime.UtcNow });
        if (kind == "outbox") db.OutboxMessages.Add(new() { Id = Guid.NewGuid(), MessageKey = "other-owner", MessageType = "incident", PayloadJson = $"{{\"incidentId\":\"{incidentId}\"}}", OccurredAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseSeeder.CleanupPortfolioDemoAsync(db, "Test", apply: true));
        Assert.Contains("Cleanup blocked", failure.Message);
        Assert.Equal(6, await db.Incidents.CountAsync());
        Assert.Equal(6, await db.Resources.CountAsync());
        Assert.Equal(3, await db.Notifications.CountAsync(x => x.DeduplicationKey != null));
    }

    [Fact]
    public async Task ManifestTampering_CannotAdoptForeignIds()
    {
        await using var fixture = await CreateFixtureAsync();
        var db = fixture.Context;
        await DatabaseSeeder.SeedPortfolioDemoAsync(db, Options);
        var marker = await db.AuditLogs.SingleAsync();
        var json = JsonNode.Parse(marker.NewValues!)!;
        json["Records"]![nameof(Incident)]![0] = Guid.NewGuid().ToString();
        marker.NewValues = json.ToJsonString();
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseSeeder.CleanupPortfolioDemoAsync(db, "Test", apply: true));
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseSeeder.SeedPortfolioDemoAsync(db, Options));
        Assert.Equal(6, await db.Incidents.CountAsync());
    }

    [Fact]
    public async Task Seed_RejectsMissingActorAndDoesNotSeedPartialReferenceData()
    {
        await using var fixture = await CreateFixtureAsync();
        var db = fixture.Context;
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseSeeder.SeedPortfolioDemoAsync(db, Options with { ReporterUserId = "missing" }));
        Assert.Empty(await db.Incidents.ToListAsync());
        Assert.Empty(await db.IncidentTypes.ToListAsync());
        db.IncidentTypes.Add(new() { Id = Guid.NewGuid(), Name = "Pending", Code = "PENDING", IsActive = true });
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseSeeder.SeedPortfolioDemoAsync(db, Options));
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Seed_RefusesInactiveActorsWithoutCreatingRecords(bool reporter)
    {
        await using var fixture = await CreateFixtureAsync();
        var db = fixture.Context;
        (await db.Users.FindAsync(reporter ? Options.ReporterUserId : Options.OperatorUserId))!.IsActive = false;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseSeeder.SeedPortfolioDemoAsync(db, Options));
        Assert.Empty(await db.Incidents.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Seed_NumberCollisionCannotAdoptAnUnownedIncident()
    {
        await using var fixture = await CreateFixtureAsync();
        var db = fixture.Context;
        await DatabaseSeeder.SeedAsync(db);
        var foreign = new Incident
        {
            Id = Guid.NewGuid(), IncidentNumber = "DEMO18-FLOOD-01", Title = "Other owner's report",
            Description = "Do not adopt this report", IncidentTypeId = await db.IncidentTypes.Select(x => x.Id).FirstAsync()
        };
        db.Incidents.Add(foreign);
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => DatabaseSeeder.SeedPortfolioDemoAsync(db, Options));
        Assert.Contains("collision", error.Message);
        Assert.Equal(foreign.Id, (await db.Incidents.SingleAsync()).Id);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task Cleanup_WithoutOwnershipEvidenceDoesNotDeleteByPrefix()
    {
        await using var fixture = await CreateFixtureAsync();
        var db = fixture.Context;
        await DatabaseSeeder.SeedAsync(db);
        db.Incidents.Add(new Incident
        {
            IncidentNumber = "DEMO18-FLOOD-01", Title = "Other owner's report", Description = "No importer manifest",
            IncidentTypeId = await db.IncidentTypes.Select(x => x.Id).FirstAsync()
        });
        await db.SaveChangesAsync();
        var result = await DatabaseSeeder.CleanupPortfolioDemoAsync(db, "Test", apply: true);
        Assert.False(result.Applied);
        Assert.Empty(result.RecordCounts);
        Assert.Single(await db.Incidents.ToListAsync());
    }

    [Theory]
    [InlineData("Production", "Server=localhost;Database=EmergencyResponse_PortfolioDemo;Integrated Security=True")]
    [InlineData("Staging", "Server=localhost;Database=EmergencyResponse_PortfolioDemo;Integrated Security=True")]
    [InlineData("Development", "Server=localhost;Database=EmergencyResponse_LoadTests;Integrated Security=True")]
    [InlineData("Development", "Server=localhost;Database=EmergencyResponse_IntegrationTests;Integrated Security=True")]
    [InlineData("Development", "Server=foreign.database.windows.net;Database=EmergencyResponse_PortfolioDemo;Integrated Security=True")]
    public void Guard_RefusesUnsafeTargetsBeforeAnyConnection(string environment, string connection)
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
        Assert.Throws<InvalidOperationException>(() => DatabaseSeeder.ValidatePortfolioDemoTarget(db, environment));
    }

    private static async Task<SqliteApplicationDbFixture> CreateFixtureAsync()
    {
        var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var db = fixture.Context;
        db.Users.AddRange(new ApplicationUser { Id = Options.ReporterUserId, UserName = "DEMO18-citizen", NormalizedUserName = "DEMO18-CITIZEN", FirstName = "Demo", LastName = "Citizen" },
            new ApplicationUser { Id = Options.OperatorUserId, UserName = "DEMO18-operator", NormalizedUserName = "DEMO18-OPERATOR", FirstName = "Demo", LastName = "Operator" });
        db.Roles.AddRange(new IdentityRole { Id = "citizen-role", Name = "Citizen", NormalizedName = "CITIZEN" }, new IdentityRole { Id = "operator-role", Name = "Operator", NormalizedName = "OPERATOR" });
        db.UserRoles.AddRange(new IdentityUserRole<string> { UserId = Options.ReporterUserId, RoleId = "citizen-role" }, new IdentityUserRole<string> { UserId = Options.OperatorUserId, RoleId = "operator-role" });
        await db.SaveChangesAsync();
        return fixture;
    }
}
