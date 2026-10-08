using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Predictive.Training;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.UnitTests.Predictive;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class LabelGeneratorTests
{
    [Fact]
    public async Task EscalationWithinSixHours_IsPositive()
    {
        await using var fixture = await Fixture.CreateAsync();
        var snapshotAt = DateTime.UtcNow.AddHours(-8);
        var escalation = fixture.AddEscalation(snapshotAt.AddHours(2));

        var result = await fixture.Generator.GenerateAsync(
            fixture.IncidentId,
            snapshotAt);

        Assert.True(result.Label);
        Assert.Equal(escalation.Id, result.OutcomeEscalationId);
    }

    [Fact]
    public async Task EscalationAfterSixHours_IsNegative()
    {
        await using var fixture = await Fixture.CreateAsync();
        var snapshotAt = DateTime.UtcNow.AddHours(-8);
        fixture.AddEscalation(snapshotAt.AddHours(7));

        var result = await fixture.Generator.GenerateAsync(
            fixture.IncidentId,
            snapshotAt);

        Assert.False(result.Label);
        Assert.Null(result.OutcomeEscalationId);
    }

    [Fact]
    public async Task EscalationAtExactlySixHours_IsPositive()
    {
        await using var fixture = await Fixture.CreateAsync();
        var snapshotAt = DateTime.UtcNow.AddHours(-8);
        var escalation = fixture.AddEscalation(snapshotAt.AddHours(6));

        var result = await fixture.Generator.GenerateAsync(
            fixture.IncidentId,
            snapshotAt);

        Assert.True(result.Label);
        Assert.Equal(escalation.Id, result.OutcomeEscalationId);
    }

    [Fact]
    public async Task EscalationAtSnapshotTime_IsNegative()
    {
        await using var fixture = await Fixture.CreateAsync();
        var snapshotAt = DateTime.UtcNow.AddHours(-8);
        fixture.AddEscalation(snapshotAt);

        var result = await fixture.Generator.GenerateAsync(
            fixture.IncidentId,
            snapshotAt);

        Assert.False(result.Label);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ApplicationDbContext _dbContext;

        private Fixture(
            SqliteConnection connection,
            ApplicationDbContext dbContext,
            Guid incidentId)
        {
            _connection = connection;
            _dbContext = dbContext;
            IncidentId = incidentId;
            Generator = new EscalationLabelGenerator(_dbContext);
        }

        public Guid IncidentId { get; }

        public EscalationLabelGenerator Generator { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var dbContext = new ApplicationDbContext(options);
            await dbContext.Database.EnsureCreatedAsync();
            var incidentId = Guid.NewGuid();
            var incidentTypeId = Guid.NewGuid();
            dbContext.IncidentTypes.Add(new IncidentType
            {
                Id = incidentTypeId,
                Name = "Test",
                Code = "TEST"
            });
            dbContext.Incidents.Add(new Incident
            {
                Id = incidentId,
                IncidentNumber = "INC-TEST",
                Title = "Test",
                Description = "Test",
                IncidentTypeId = incidentTypeId,
                ReportedAtUtc = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
            return new Fixture(connection, dbContext, incidentId);
        }

        public IncidentEscalation AddEscalation(DateTime createdAtUtc)
        {
            var escalation = new IncidentEscalation
            {
                Id = Guid.NewGuid(),
                IncidentId = IncidentId,
                Type = Domain.Enums.EscalationType.SeverityIncrease,
                Level = Domain.Enums.EscalationLevel.High,
                Reason = "Test",
                CreatedAtUtc = createdAtUtc
            };
            _dbContext.IncidentEscalations.Add(escalation);
            _dbContext.SaveChanges();
            return escalation;
        }

        public async ValueTask DisposeAsync()
        {
            await _dbContext.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
