using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.UnitTests.Infrastructure;

public sealed class HostedPortfolioSeederTests
{
    [Fact]
    public async Task PreviewIsReadOnly_SeedIsRepeatable_AndRelationshipsAreComplete()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var db = fixture.Context;
        await DatabaseSeeder.SeedAsync(db);
        var referenceTypes = await db.IncidentTypes.CountAsync();
        var departments = await db.Departments.CountAsync();
        var preview = await HostedPortfolioSeeder.PreviewAsync(db);
        Assert.Equal(24, preview.PlannedInserts["Incidents"]);
        Assert.Equal(72, preview.PlannedInserts["Histories"]);
        Assert.Empty(await db.Incidents.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
        await ApplyApprovedAsync(db, HostedPortfolioSeeder.Authorization);
        var incident = await db.Incidents.FirstAsync();
        incident.Title = "Reviewed exercise title";
        await db.SaveChangesAsync();
        await ApplyApprovedAsync(db, HostedPortfolioSeeder.Authorization);
        Assert.Equal("Reviewed exercise title", incident.Title);
        Assert.Equal(24, await db.Incidents.CountAsync());
        Assert.Equal(24, await db.IncidentLocations.CountAsync());
        Assert.Equal(72, await db.IncidentStatusHistory.CountAsync());
        Assert.Single(await db.AuditLogs.ToListAsync());
        Assert.Equal(referenceTypes, await db.IncidentTypes.CountAsync());
        Assert.Equal(departments, await db.Departments.CountAsync());
        Assert.Empty(await db.Users.ToListAsync());
        Assert.Empty(await db.Resources.ToListAsync());
        Assert.Empty(await db.IncidentAssignments.ToListAsync());
        Assert.Empty(await db.IncidentEscalations.ToListAsync());
        Assert.Empty(await db.OperationalSituations.ToListAsync());
        Assert.Empty(await db.Notifications.ToListAsync());
        Assert.Empty(await db.OutboxMessages.ToListAsync());
        Assert.Empty(await db.AIAnalyses.ToListAsync());
        Assert.Empty(await db.VisualAnalyses.ToListAsync());
        Assert.Empty(await db.IncidentDuplicateMatches.ToListAsync());
        var rows = await db.Incidents.Include(x => x.Locations).Include(x => x.StatusHistory).ToListAsync();
        Assert.Equal(6, rows.Select(x => x.IncidentTypeId).Distinct().Count());
        Assert.All(rows, row =>
        {
            Assert.Single(row.Locations, x => x.IsPrimary);
            Assert.Equal(row.Status, row.StatusHistory.OrderBy(x => x.ChangedAtUtc).Last().NewStatus);
            Assert.All(row.StatusHistory, history => Assert.Equal(row.Id, history.IncidentId));
            Assert.Null(row.ReporterUserId);
            Assert.Null(row.Casualties);
            Assert.Null(row.EmbeddingModel);
            Assert.Equal(DuplicateDetectionStatus.NotStarted, row.DuplicateDetectionStatus);
        });
        Assert.True((await HostedPortfolioSeeder.PreviewAsync(db)).AlreadySeeded);
    }

    [Fact]
    public async Task ForeignNumberCollisionIsNeverAdopted()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var db = fixture.Context;
        await DatabaseSeeder.SeedAsync(db);
        var foreign = new EmergencyResponse.Domain.Entities.Incident
        {
            Id = Guid.NewGuid(), IncidentNumber = "SYN24-01", Title = "Existing record",
            Description = "Preserve this record", IncidentTypeId = await db.IncidentTypes.Select(x => x.Id).FirstAsync()
        };
        db.Incidents.Add(foreign);
        await db.SaveChangesAsync();
        Assert.Equal(HostedPortfolioFailure.Ownership, (await Assert.ThrowsAsync<HostedPortfolioException>(() => ApplyApprovedAsync(db, HostedPortfolioSeeder.Authorization))).Failure);
        Assert.Equal(foreign.Id, (await db.Incidents.SingleAsync()).Id);
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task AuthorizationAndMissingReferenceDataFailBeforeWrites()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        Assert.Equal(HostedPortfolioFailure.Confirmation, (await Assert.ThrowsAsync<HostedPortfolioException>(() => ApplyApprovedAsync(fixture.Context, ""))).Failure);
        Assert.Equal(HostedPortfolioFailure.ReferenceData, (await Assert.ThrowsAsync<HostedPortfolioException>(() => HostedPortfolioSeeder.PreviewAsync(fixture.Context))).Failure);
        Assert.Empty(await fixture.Context.Incidents.ToListAsync());
    }

    [Fact]
    public async Task PartialDatasetFailsClosedInsteadOfRepairingOrOverwriting()
    {
        await using var fixture = await SqliteApplicationDbFixture.CreateAsync();
        var db = fixture.Context;
        await DatabaseSeeder.SeedAsync(db);
        await ApplyApprovedAsync(db, HostedPortfolioSeeder.Authorization);
        db.IncidentLocations.Remove(await db.IncidentLocations.FirstAsync());
        await db.SaveChangesAsync();
        Assert.Equal(HostedPortfolioFailure.Integrity, (await Assert.ThrowsAsync<HostedPortfolioException>(() => ApplyApprovedAsync(db, HostedPortfolioSeeder.Authorization))).Failure);
        Assert.Equal(23, await db.IncidentLocations.CountAsync());
    }

    [Theory]
    [InlineData("Server=localhost;Database=EmergencyResponseDb;Integrated Security=True")]
    [InlineData("Server=other.database.windows.net;Database=EmergencyResponse;Authentication=Active Directory Default")]
    [InlineData("Server=sql-er-demo-qqgqkekj.database.windows.net;Database=master;Authentication=Active Directory Default")]
    [InlineData("Server=sql-er-demo-qqgqkekj.database.windows.net;Database=EmergencyResponse;User ID=sa;Password=not-a-real-secret")]
    public void RefusesUnapprovedTargets(string connection) =>
        Assert.Throws<HostedPortfolioException>(() => HostedPortfolioSeeder.ValidateConnection(connection));

    private static async Task<HostedPortfolioPreview> ApplyApprovedAsync(
        EmergencyResponse.Infrastructure.Persistence.ApplicationDbContext db, string authorization)
    {
        var fingerprint = authorization == HostedPortfolioSeeder.Authorization
            ? (await HostedPortfolioSeeder.PreviewAsync(db)).Fingerprint : "HPFP1:" + new string('A', 64);
        return await HostedPortfolioSeeder.ApplyCoreAsync(db, authorization, fingerprint, HostedPortfolioApprovalTests.Sid,
            (_, _) => Task.FromResult(new HostedPortfolioSession(HostedPortfolioApprovalTests.Sid, "E", true, true)));
    }
}
