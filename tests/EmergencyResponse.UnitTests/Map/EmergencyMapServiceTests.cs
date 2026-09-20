using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Map;
using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EmergencyResponse.UnitTests.Map;

public sealed class EmergencyMapServiceTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly MapTestDbContext _context;
    private readonly EmergencyMapService _service;

    public EmergencyMapServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new MapTestDbContext(options);
        _context.Database.EnsureCreated();

        _service = new EmergencyMapService(_context);
    }

    [Fact]
    public async Task GetIncidentsAsync_OnlyReturnsActiveIncidentsWithLocations()
    {
        // Arrange
        var incidentType = new IncidentType
        {
            Id = Guid.NewGuid(),
            Name = "Fire Emergency",
            Code = "FIRE",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.IncidentTypes.Add(incidentType);

        // 1. Active with Location (Should be returned)
        var activeIncident = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = "INC-ACTIVE-1",
            Title = "Active Fire",
            Description = "Test active fire incident description",
            IncidentTypeId = incidentType.Id,
            Severity = SeverityLevel.Critical,
            Priority = PriorityLevel.Critical,
            Status = IncidentStatus.InProgress,
            ReportedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
        activeIncident.Locations.Add(new IncidentLocation
        {
            Id = Guid.NewGuid(),
            IncidentId = activeIncident.Id,
            Latitude = 28.6139m,
            Longitude = 77.2090m,
            Address = "New Delhi",
            IsPrimary = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        _context.Incidents.Add(activeIncident);

        // 2. Closed with Location (Should be excluded)
        var closedIncident = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = "INC-CLOSED-1",
            Title = "Closed Fire",
            Description = "Test closed fire incident description",
            IncidentTypeId = incidentType.Id,
            Severity = SeverityLevel.Low,
            Priority = PriorityLevel.Low,
            Status = IncidentStatus.Closed,
            ReportedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
        closedIncident.Locations.Add(new IncidentLocation
        {
            Id = Guid.NewGuid(),
            IncidentId = closedIncident.Id,
            Latitude = 19.0760m,
            Longitude = 72.8777m,
            Address = "Mumbai",
            IsPrimary = true,
            CreatedAtUtc = DateTime.UtcNow
        });
        _context.Incidents.Add(closedIncident);

        // 3. Active WITHOUT Location (Should be excluded)
        var noLocationIncident = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = "INC-NOLOC-1",
            Title = "No Location Fire",
            Description = "Test no location incident description",
            IncidentTypeId = incidentType.Id,
            Severity = SeverityLevel.Moderate,
            Priority = PriorityLevel.Normal,
            Status = IncidentStatus.Submitted,
            ReportedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Incidents.Add(noLocationIncident);

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetIncidentsAsync();

        // Assert
        Assert.Single(result);
        var item = result[0];
        Assert.Equal(activeIncident.Id, item.Id);
        Assert.Equal("INC-ACTIVE-1", item.IncidentNumber);
        Assert.Equal("Active Fire", item.Title);
        Assert.Equal(28.6139, item.Latitude, precision: 4);
        Assert.Equal(77.2090, item.Longitude, precision: 4);
        Assert.Equal("New Delhi", item.Address);
    }

    [Fact]
    public async Task GetIncidentsAsync_SelectsPrimaryLocation_WhenMultipleLocationsExist()
    {
        // Arrange
        var incidentType = new IncidentType
        {
            Id = Guid.NewGuid(),
            Name = "Medical Emergency",
            Code = "MED",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.IncidentTypes.Add(incidentType);

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = "INC-MULTI-LOC",
            Title = "Multi Location",
            Description = "Test multi-location incident description",
            IncidentTypeId = incidentType.Id,
            Severity = SeverityLevel.High,
            Priority = PriorityLevel.High,
            Status = IncidentStatus.UnderReview,
            ReportedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };

        // Non-primary location
        incident.Locations.Add(new IncidentLocation
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            Latitude = 12.9716m,
            Longitude = 77.5946m,
            Address = "Secondary Location",
            IsPrimary = false,
            CreatedAtUtc = DateTime.UtcNow
        });

        // Primary location
        incident.Locations.Add(new IncidentLocation
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            Latitude = 13.0827m,
            Longitude = 80.2707m,
            Address = "Primary Location",
            IsPrimary = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetIncidentsAsync();

        // Assert
        Assert.Single(result);
        var item = result[0];
        Assert.Equal(13.0827, item.Latitude, precision: 4);
        Assert.Equal(80.2707, item.Longitude, precision: 4);
        Assert.Equal("Primary Location", item.Address);
    }

    [Fact]
    public async Task GetIncidentsAsync_DetectsReviewFlagsProperly()
    {
        // Arrange
        var incidentType = new IncidentType
        {
            Id = Guid.NewGuid(),
            Name = "Rescue",
            Code = "RESC",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.IncidentTypes.Add(incidentType);

        var inc1 = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = "INC-FLAGGED-1",
            Title = "Flagged Incident",
            Description = "Test flagged incident description",
            IncidentTypeId = incidentType.Id,
            Severity = SeverityLevel.Critical,
            Priority = PriorityLevel.Critical,
            Status = IncidentStatus.Submitted,
            ReportedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
        inc1.Locations.Add(new IncidentLocation
        {
            Id = Guid.NewGuid(),
            IncidentId = inc1.Id,
            Latitude = 20.0m,
            Longitude = 75.0m,
            IsPrimary = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        var inc2 = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = "INC-CANDIDATE-2",
            Title = "Duplicate Candidate",
            Description = "Test duplicate candidate incident description",
            IncidentTypeId = incidentType.Id,
            Severity = SeverityLevel.Critical,
            Priority = PriorityLevel.Critical,
            Status = IncidentStatus.Submitted,
            ReportedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
        inc2.Locations.Add(new IncidentLocation
        {
            Id = Guid.NewGuid(),
            IncidentId = inc2.Id,
            Latitude = 20.1m,
            Longitude = 75.1m,
            IsPrimary = true,
            CreatedAtUtc = DateTime.UtcNow
        });

        _context.Incidents.AddRange(inc1, inc2);

        // Add AI analysis requiring review for inc1
        _context.AIAnalyses.Add(new AIAnalysis
        {
            Id = Guid.NewGuid(),
            IncidentId = inc1.Id,
            ModelName = "gpt-4o",
            PromptVersion = "v1",
            InputReference = "test",
            OutputJson = "{}",
            ExecutionTimeMs = 100,
            Status = AIAnalysisStatus.RequiresReview,
            CreatedAtUtc = DateTime.UtcNow
        });

        // Add duplicate match where inc1 is the source incident
        _context.IncidentDuplicateMatches.Add(new IncidentDuplicateMatch
        {
            Id = Guid.NewGuid(),
            SourceIncidentId = inc1.Id,
            CandidateIncidentId = inc2.Id,
            DuplicateScore = 0.85,
            Reviewed = false,
            CreatedAtUtc = DateTime.UtcNow
        });

        // Add visual analysis requiring review for inc1
        var media = new IncidentMedia
        {
            Id = Guid.NewGuid(),
            IncidentId = inc1.Id,
            FileName = "photo.jpg",
            ContentType = "image/jpeg",
            SizeBytes = 1024,
            StorageKey = "test-key",
            StoragePath = "/tmp/photo.jpg",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.IncidentMedia.Add(media);

        _context.VisualAnalyses.Add(new VisualAnalysis
        {
            Id = Guid.NewGuid(),
            IncidentId = inc1.Id,
            IncidentMediaId = media.Id,
            ModelName = "gpt-4o",
            PromptVersion = "v1",
            Status = VisualAnalysisStatus.RequiresReview,
            HumanReviewed = false,
            CreatedAtUtc = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetIncidentsAsync();

        // Assert
        var mapped1 = result.First(x => x.Id == inc1.Id);
        Assert.True(mapped1.RequiresAIReview);
        Assert.True(mapped1.HasDuplicateCandidates);
        Assert.True(mapped1.HasVisualReview);

        var mapped2 = result.First(x => x.Id == inc2.Id);
        Assert.False(mapped2.RequiresAIReview);
        Assert.False(mapped2.HasDuplicateCandidates);
        Assert.False(mapped2.HasVisualReview);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class MapTestDbContext : ApplicationDbContext
    {
        public MapTestDbContext(DbContextOptions<ApplicationDbContext> options)
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
}
