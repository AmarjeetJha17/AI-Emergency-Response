using EmergencyResponse.Application.AI.DuplicateDetection;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.AI;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.UnitTests.AI;

public sealed class IncidentConsolidationTests
{
    [Fact]
    public async Task SameIncident_ShouldBeRejected()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var incident = await fixture.AddIncidentAsync("Incident A");
        var service = fixture.CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ConsolidateDuplicateAsync(
                incident.Id,
                incident.Id,
                notes: null));

        Assert.Equal(
            "An incident cannot be consolidated with itself.",
            exception.Message);
    }

    [Fact]
    public async Task MissingCanonical_ShouldBeRejected()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var duplicate = await fixture.AddIncidentAsync("Incident B");
        var service = fixture.CreateService();
        var missingCanonicalId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ConsolidateDuplicateAsync(
                missingCanonicalId,
                duplicate.Id,
                notes: null));

        Assert.Equal(
            "One or both incidents could not be found.",
            exception.Message);
    }

    [Fact]
    public async Task MissingDuplicate_ShouldBeRejected()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var canonical = await fixture.AddIncidentAsync("Incident A");
        var service = fixture.CreateService();
        var missingDuplicateId = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.ConsolidateDuplicateAsync(
                canonical.Id,
                missingDuplicateId,
                notes: null));

        Assert.Equal(
            "One or both incidents could not be found.",
            exception.Message);
    }

    [Fact]
    public async Task AlreadyConsolidated_ShouldBeRejected()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var canonical = await fixture.AddIncidentAsync("Incident A");
        var duplicate = await fixture.AddIncidentAsync("Incident B");
        duplicate.CanonicalIncidentId = canonical.Id;
        duplicate.IsCanonical = false;
        await fixture.Context.SaveChangesAsync();

        var service = fixture.CreateService();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ConsolidateDuplicateAsync(
                canonical.Id,
                duplicate.Id,
                notes: null));

        Assert.Equal(
            "The incident has already been consolidated.",
            exception.Message);
    }

    [Fact]
    public async Task ValidDuplicate_ShouldCreateRelationAndPreserveEvidence()
    {
        await using var fixture = await TestFixture.CreateAsync();

        var canonical = await fixture.AddIncidentAsync("Canonical evidence");
        var duplicate = await fixture.AddIncidentAsync("Duplicate evidence");
        var originalDescription = duplicate.Description;
        var originalReportedAtUtc = duplicate.ReportedAtUtc;
        var originalLocation = duplicate.Locations.Single();
        var originalAddress = originalLocation.Address;
        var originalLatitude = originalLocation.Latitude;
        var originalLongitude = originalLocation.Longitude;

        var service = fixture.CreateService();

        await service.ConsolidateDuplicateAsync(
            canonical.Id,
            duplicate.Id,
            notes: "Confirmed by operator.");

        var savedCanonical = await fixture.Context.Incidents
            .SingleAsync(x => x.Id == canonical.Id);
        var savedDuplicate = await fixture.Context.Incidents
            .Include(x => x.Locations)
            .SingleAsync(x => x.Id == duplicate.Id);
        var relation = await fixture.Context.IncidentRelations
            .SingleAsync();

        Assert.False(savedDuplicate.IsCanonical);
        Assert.Equal(savedCanonical.Id, savedDuplicate.CanonicalIncidentId);

        Assert.True(savedCanonical.IsCanonical);
        Assert.Null(savedCanonical.CanonicalIncidentId);

        Assert.Equal(canonical.Id, relation.ParentIncidentId);
        Assert.Equal(duplicate.Id, relation.RelatedIncidentId);
        Assert.Equal(IncidentRelationType.Duplicate, relation.RelationType);
        Assert.Equal("Confirmed by operator.", relation.Notes);
        Assert.Equal("test-user", relation.CreatedByUserId);

        var savedLocation = savedDuplicate.Locations.Single();
        Assert.Equal(originalDescription, savedDuplicate.Description);
        Assert.Equal(originalReportedAtUtc, savedDuplicate.ReportedAtUtc);
        Assert.Equal(originalAddress, savedLocation.Address);
        Assert.Equal(originalLatitude, savedLocation.Latitude);
        Assert.Equal(originalLongitude, savedLocation.Longitude);
    }

    private sealed class TestFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestFixture(
            SqliteConnection connection,
            ApplicationDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public ApplicationDbContext Context { get; }

        public static async Task<TestFixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new ConsolidationTestDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new TestFixture(connection, context);
        }

        public IncidentConsolidationService CreateService()
        {
            return new IncidentConsolidationService(
                Context,
                new TestCurrentUserService());
        }

        public async Task<Incident> AddIncidentAsync(string description)
        {
            var incidentType = await Context.IncidentTypes
                .FirstOrDefaultAsync();

            if (incidentType is null)
            {
                incidentType = new IncidentType
                {
                    Id = Guid.NewGuid(),
                    Name = "Fire",
                    Code = "FIRE"
                };
                Context.IncidentTypes.Add(incidentType);
            }

            var incident = new Incident
            {
                Id = Guid.NewGuid(),
                IncidentNumber = $"INC-{Guid.NewGuid():N}",
                Title = "Emergency incident",
                Description = description,
                IncidentTypeId = incidentType.Id,
                Status = IncidentStatus.Submitted,
                Severity = SeverityLevel.Low,
                Priority = PriorityLevel.Normal,
                ReportedAtUtc = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc)
            };

            incident.Locations.Add(
                new IncidentLocation
                {
                    Id = Guid.NewGuid(),
                    Latitude = 28.6139m,
                    Longitude = 77.2090m,
                    Address = "Original evidence address",
                    City = "New Delhi",
                    IsPrimary = true
                });

            Context.Incidents.Add(incident);
            await Context.SaveChangesAsync();
            return incident;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class ConsolidationTestDbContext : ApplicationDbContext
    {
        public ConsolidationTestDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Incident>().Ignore(x => x.Embedding);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.GetColumnType() is "nvarchar(max)" or "varchar(max)")
                    {
                        modelBuilder.Entity(entityType.ClrType)
                            .Property(property.Name)
                            .HasColumnType("TEXT");
                    }
                }
            }
        }
    }

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        public string? UserId => "test-user";

        public bool IsAuthenticated => true;

        public bool IsInRole(string role) => true;
    }
}
