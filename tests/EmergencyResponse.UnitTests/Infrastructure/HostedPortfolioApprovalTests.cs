using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Identity;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.UnitTests.Infrastructure;

public sealed class HostedPortfolioApprovalTests
{
    internal const string Sid = "0x11111111111111111111111111111111";
    private static Task<HostedPortfolioSession> Session(ApplicationDbContext db, CancellationToken ct) =>
        Task.FromResult(new HostedPortfolioSession(Sid, "E", true, true));

    private static Task<HostedPortfolioPreview> Apply(ApplicationDbContext db, string fingerprint,
        Func<ApplicationDbContext, CancellationToken, Task<HostedPortfolioSession>>? session = null,
        Func<Task>? afterSave = null) => HostedPortfolioSeeder.ApplyCoreAsync(db, HostedPortfolioSeeder.Authorization,
            fingerprint, Sid, session ?? Session, afterSave);

    private static async Task<SqliteApplicationDbFixture> Fixture()
    {
        var fixture = await SqliteApplicationDbFixture.CreateAsync();
        await DatabaseSeeder.SeedAsync(fixture.Context);
        return fixture;
    }

    [Fact]
    public async Task FingerprintIsDeterministicAndRepeatImportRemainsNoOp()
    {
        await using var fixture = await Fixture();
        var db = fixture.Context;
        var first = await HostedPortfolioSeeder.PreviewAsync(db);
        Assert.Equal(first.Fingerprint, (await HostedPortfolioSeeder.PreviewAsync(db)).Fingerprint);
        await Apply(db, first.Fingerprint);
        var row = await db.Incidents.FirstAsync();
        row.Title = "Operator edit retained";
        await db.SaveChangesAsync();
        var repeat = await HostedPortfolioSeeder.PreviewAsync(db);
        Assert.True(repeat.AlreadySeeded);
        await Apply(db, repeat.Fingerprint);
        Assert.Equal("Operator edit retained", (await db.Incidents.FirstAsync(x => x.Id == row.Id)).Title);
        Assert.Equal(24, await db.Incidents.CountAsync());
        Assert.Equal(24, await db.IncidentLocations.CountAsync());
        Assert.Equal(72, await db.IncidentStatusHistory.CountAsync());
        Assert.Single(await db.AuditLogs.ToListAsync());
    }

    [Theory]
    [InlineData("incident")]
    [InlineData("type")]
    [InlineData("department")]
    [InlineData("user")]
    [InlineData("requirement")]
    public async Task ChangesAfterPreviewAbortBeforeInserts(string change)
    {
        await using var fixture = await Fixture();
        var db = fixture.Context;
        var approved = await HostedPortfolioSeeder.PreviewAsync(db);
        switch (change)
        {
            case "incident": db.Incidents.Add(new Incident { Id = Guid.NewGuid(), IncidentNumber = "FOREIGN-01",
                Title = "Existing", Description = "Preserve", IncidentTypeId = await db.IncidentTypes.Select(x => x.Id).FirstAsync() }); break;
            case "type": (await db.IncidentTypes.FirstAsync()).Name = "Reference edit"; break;
            case "department": (await db.Departments.FirstAsync()).Name = "Reference edit"; break;
            case "user": db.Users.Add(new ApplicationUser { Id = "foreign-user", UserName = "existing", FirstName = "Existing", LastName = "User" }); break;
            case "requirement": (await db.IncidentTypeResourceRequirements.FirstAsync()).MinimumCount++; break;
        }
        await db.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<HostedPortfolioException>(() => Apply(db, approved.Fingerprint));
        Assert.Equal(HostedPortfolioFailure.Approval, error.Failure);
        Assert.False(await db.Incidents.AnyAsync(x => x.IncidentNumber.StartsWith("SYN24-")));
        Assert.Empty(await db.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task OwnershipMarkerContentChangeInvalidatesApproval()
    {
        await using var fixture = await Fixture();
        var db = fixture.Context;
        await Apply(db, (await HostedPortfolioSeeder.PreviewAsync(db)).Fingerprint);
        var approved = await HostedPortfolioSeeder.PreviewAsync(db);
        (await db.AuditLogs.SingleAsync()).NewValues = "Modified ownership metadata";
        await db.SaveChangesAsync();
        Assert.Equal(HostedPortfolioFailure.Approval,
            (await Assert.ThrowsAsync<HostedPortfolioException>(() => Apply(db, approved.Fingerprint))).Failure);
        Assert.Equal(24, await db.Incidents.CountAsync());
    }

    [Fact]
    public async Task MismatchedFingerprintNeverInserts()
    {
        await using var fixture = await Fixture();
        Assert.Equal(HostedPortfolioFailure.Approval,
            (await Assert.ThrowsAsync<HostedPortfolioException>(() => Apply(fixture.Context, "HPFP1:" + new string('0', 64)))).Failure);
        Assert.Empty(await fixture.Context.Incidents.ToListAsync());
    }

    [Theory]
    [InlineData("location")]
    [InlineData("history")]
    [InlineData("extra")]
    [InlineData("partial")]
    public async Task DependentCorruptionStopsWithoutRepair(string corruption)
    {
        await using var fixture = await Fixture();
        var db = fixture.Context;
        await Apply(db, (await HostedPortfolioSeeder.PreviewAsync(db)).Fingerprint);
        var approved = await HostedPortfolioSeeder.PreviewAsync(db);
        var parents = await db.Incidents.Select(x => x.Id).ToListAsync();
        if (corruption == "location")
        {
            var row = await db.IncidentLocations.FirstAsync();
            row.IncidentId = parents.First(x => x != row.IncidentId);
        }
        if (corruption == "history")
        {
            var row = await db.IncidentStatusHistory.FirstAsync();
            row.IncidentId = parents.First(x => x != row.IncidentId);
        }
        if (corruption == "extra") db.IncidentLocations.Add(new IncidentLocation { Id = Guid.NewGuid(), IncidentId = parents[0] });
        if (corruption == "partial") db.IncidentStatusHistory.Remove(await db.IncidentStatusHistory.FirstAsync());
        await db.SaveChangesAsync();
        var count = await db.IncidentLocations.CountAsync();
        Assert.Equal(HostedPortfolioFailure.Integrity,
            (await Assert.ThrowsAsync<HostedPortfolioException>(() => Apply(db, approved.Fingerprint))).Failure);
        Assert.Equal(count, await db.IncidentLocations.CountAsync());
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("group")]
    [InlineData("impersonated")]
    [InlineData("permissions")]
    public async Task SessionMustMatchExpectedPrincipalAndPermissions(string reason)
    {
        await using var fixture = await Fixture();
        var db = fixture.Context;
        var fingerprint = (await HostedPortfolioSeeder.PreviewAsync(db)).Fingerprint;
        var bad = new HostedPortfolioSession(reason == "wrong" ? "0x22222222222222222222222222222222" : Sid,
            reason == "group" ? "X" : "E", reason != "impersonated", reason != "permissions");
        var error = await Assert.ThrowsAsync<HostedPortfolioException>(() => Apply(db, fingerprint,
            (context, _) => { Assert.NotNull(context.Database.CurrentTransaction); return Task.FromResult(bad); }));
        Assert.Equal(reason == "permissions" ? HostedPortfolioFailure.Permissions : HostedPortfolioFailure.Principal, error.Failure);
        Assert.Empty(await db.Incidents.ToListAsync());
    }

    [Fact]
    public async Task RealTransactionRollsBackAll121RowsAfterSaveFailure()
    {
        await using var fixture = await Fixture();
        var db = fixture.Context;
        var fingerprint = (await HostedPortfolioSeeder.PreviewAsync(db)).Fingerprint;
        await Assert.ThrowsAsync<IOException>(() => Apply(db, fingerprint, afterSave: async () =>
        {
            Assert.Equal(24, await db.Incidents.CountAsync());
            Assert.Equal(24, await db.IncidentLocations.CountAsync());
            Assert.Equal(72, await db.IncidentStatusHistory.CountAsync());
            Assert.Single(await db.AuditLogs.ToListAsync());
            throw new IOException("Injected failure before commit");
        }));
        db.ChangeTracker.Clear();
        Assert.Empty(await db.Incidents.ToListAsync());
        Assert.Empty(await db.IncidentLocations.ToListAsync());
        Assert.Empty(await db.IncidentStatusHistory.ToListAsync());
        Assert.Empty(await db.AuditLogs.ToListAsync());
        Assert.Equal(10, await db.IncidentTypes.CountAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("HPFP1:abc")]
    [InlineData("HPFP2:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")]
    [InlineData("HPFP1:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\n")]
    public void MalformedApprovalsRejected(string fingerprint) =>
        Assert.Throws<HostedPortfolioException>(() => HostedPortfolioSeeder.ValidateFingerprint(fingerprint));

    [Fact]
    public void ApprovalInputRequiresExplicitUnambiguousArguments()
    {
        Assert.Throws<HostedPortfolioException>(() => HostedPortfolioApprovalInput.Parse(["hosted-seed", "--expected-principal-sid", Sid]));
        Assert.Throws<HostedPortfolioException>(() => HostedPortfolioApprovalInput.Parse(["hosted-preview", "--expected-principal-sid", Sid, "--expected-principal-sid", Sid]));
        Assert.Throws<HostedPortfolioException>(() => HostedPortfolioApprovalInput.Parse(["hosted-preview", "--expected-principal-sid", Sid + "\n"]));
        Assert.Equal(Sid, HostedPortfolioApprovalInput.Parse(["hosted-preview", "--expected-principal-sid", Sid]).ExpectedPrincipalSid);
    }
}
