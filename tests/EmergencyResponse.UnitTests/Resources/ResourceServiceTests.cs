using EmergencyResponse.Application.Events;
using EmergencyResponse.Application.Interfaces;
using EmergencyResponse.Application.Realtime;
using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Resources;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace EmergencyResponse.UnitTests.Resources;

public sealed class ResourceServiceTests : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ResourceTestDbContext _context;
    private readonly FakeCurrentUserService _currentUserService;
    private readonly FakeIncidentNotificationService _notificationService;
    private readonly ResourceService _service;

    public ResourceServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ResourceTestDbContext(options);
        _context.Database.EnsureCreated();

        _currentUserService = new FakeCurrentUserService
        {
            UserId = "user-test-123",
            Roles = new List<string> { "Operator" }
        };
        _notificationService = new FakeIncidentNotificationService();

        _service = new ResourceService(
            _context,
            _currentUserService,
            _notificationService,
            NullLogger<ResourceService>.Instance);
    }

    private async Task<Department> CreateDepartmentAsync(string name = "Fire & Rescue", string code = "FIRE")
    {
        var dept = new Department
        {
            Id = Guid.NewGuid(),
            Name = name,
            Code = code,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();
        return dept;
    }

    private async Task<IncidentType> EnsureIncidentTypeAsync()
    {
        var existing = await _context.IncidentTypes.FirstOrDefaultAsync();
        if (existing != null) return existing;

        var type = new IncidentType
        {
            Id = Guid.NewGuid(),
            Name = "General Emergency",
            Code = "GEN",
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.IncidentTypes.Add(type);
        await _context.SaveChangesAsync();
        return type;
    }

    private async Task<Incident> CreateIncidentAsync(Guid departmentId, string title = "Emergency Incident")
    {
        var type = await EnsureIncidentTypeAsync();
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentNumber = "INC-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Title = title,
            Description = "Test incident description",
            DepartmentId = departmentId,
            IncidentTypeId = type.Id,
            ReportedAtUtc = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Incidents.Add(incident);
        await _context.SaveChangesAsync();
        return incident;
    }

    private async Task<Resource> CreateResourceAsync(
        Guid departmentId,
        ResourceStatus status = ResourceStatus.Available,
        ResourceType type = ResourceType.Ambulance,
        string name = "Medic 1")
    {
        var resource = new Resource
        {
            Id = Guid.NewGuid(),
            Name = name,
            Type = type,
            Status = status,
            DepartmentId = departmentId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();
        return resource;
    }

    [Fact]
    public async Task AssignAsync_WhenValid_AssignsResourceCreatesAssignmentAndAudit()
    {
        var dept = await CreateDepartmentAsync("Fire & Rescue", "FIRE");
        var incident = await CreateIncidentAsync(dept.Id, "Kitchen Fire");
        var resource = await CreateResourceAsync(dept.Id, ResourceStatus.Available, ResourceType.FireTruck, "Engine 5");

        var result = await _service.AssignAsync(resource.Id, incident.Id, "Immediate dispatch requested");

        Assert.NotNull(result);
        Assert.Equal(resource.Id, result.ResourceId);
        Assert.Equal(incident.Id, result.IncidentId);

        var updatedResource = await _context.Resources.FindAsync(resource.Id);
        Assert.Equal(ResourceStatus.Assigned, updatedResource!.Status);

        var assignment = await _context.ResourceAssignments.FindAsync(result.AssignmentId);
        Assert.NotNull(assignment);
        Assert.True(assignment!.IsActive);
        Assert.Equal(ResourceAssignmentStatus.Active, assignment.Status);
        Assert.Equal("Immediate dispatch requested", assignment.Remarks);
        Assert.Equal("user-test-123", assignment.AssignedByUserId);

        var history = await _context.ResourceStatusHistories.FirstOrDefaultAsync(x => x.ResourceId == resource.Id);
        Assert.NotNull(history);
        Assert.Equal(ResourceStatus.Available, history!.PreviousStatus);
        Assert.Equal(ResourceStatus.Assigned, history.NewStatus);

        var audit = await _context.AuditLogs.FirstOrDefaultAsync(x => x.Action == "ResourceAssigned");
        Assert.NotNull(audit);
        Assert.Equal("user-test-123", audit!.UserId);

        Assert.Single(_notificationService.ResourceEvents);
        Assert.Equal(ResourceUpdateReasons.Assigned, _notificationService.ResourceEvents[0].Reason);
    }

    [Fact]
    public async Task AssignAsync_WhenResourceNotAvailable_ThrowsInvalidOperationException()
    {
        var dept = await CreateDepartmentAsync("Medical Corps", "MED");
        var incident = await CreateIncidentAsync(dept.Id, "Heart Attack");
        var resource = await CreateResourceAsync(dept.Id, ResourceStatus.Maintenance, ResourceType.Ambulance, "Medic 1");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AssignAsync(resource.Id, incident.Id, null));
    }

    [Fact]
    public async Task AssignAsync_WhenActiveAssignmentExists_ThrowsInvalidOperationException()
    {
        var dept = await CreateDepartmentAsync("Medical Corps", "MED");
        var incident = await CreateIncidentAsync(dept.Id, "Allergic Reaction");
        var resource = await CreateResourceAsync(dept.Id, ResourceStatus.Available, ResourceType.Ambulance, "Medic 2");

        var existingAssignment = new ResourceAssignment
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            IncidentId = incident.Id,
            AssignedAtUtc = DateTime.UtcNow,
            IsActive = true,
            Status = ResourceAssignmentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ResourceAssignments.Add(existingAssignment);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AssignAsync(resource.Id, incident.Id, null));
    }

    [Fact]
    public async Task AssignAsync_WhenDepartmentMismatchAndOperator_ThrowsInvalidOperationException()
    {
        var deptFire = await CreateDepartmentAsync("Fire Department", "FIRE");
        var deptPolice = await CreateDepartmentAsync("Police Department", "POL");

        var incident = await CreateIncidentAsync(deptPolice.Id, "Armed Robbery");
        var resource = await CreateResourceAsync(deptFire.Id, ResourceStatus.Available, ResourceType.FireTruck, "Ladder 1");

        _currentUserService.Roles = new List<string> { "Operator" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AssignAsync(resource.Id, incident.Id, null));

        Assert.Equal("The resource does not belong to the incident's department.", ex.Message);
    }

    [Fact]
    public async Task AssignAsync_WhenDepartmentMismatchAndAdmin_Succeeds()
    {
        var deptFire = await CreateDepartmentAsync("Fire Department", "FIRE");
        var deptPolice = await CreateDepartmentAsync("Police Department", "POL");

        var incident = await CreateIncidentAsync(deptPolice.Id, "Search and Rescue");
        var resource = await CreateResourceAsync(deptFire.Id, ResourceStatus.Available, ResourceType.Boat, "Rescue Boat 1");

        _currentUserService.Roles = new List<string> { "EmergencyAdmin" };

        var result = await _service.AssignAsync(resource.Id, incident.Id, "Cross-department assignment approved");
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenValidTransition_UpdatesResourceAndAssignmentTimestamps()
    {
        var dept = await CreateDepartmentAsync("EMS", "EMS");
        var incident = await CreateIncidentAsync(dept.Id, "Car Crash");
        var resource = await CreateResourceAsync(dept.Id, ResourceStatus.Assigned, ResourceType.Ambulance, "Ambulance 9");

        var assignment = new ResourceAssignment
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            IncidentId = incident.Id,
            AssignedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            IsActive = true,
            Status = ResourceAssignmentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ResourceAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        var result = await _service.ChangeStatusAsync(
            resource.Id,
            ResourceStatus.Dispatched,
            assignment.IncidentId,
            "Dispatched from station");

        Assert.Equal(ResourceStatus.Assigned, result.PreviousStatus);
        Assert.Equal(ResourceStatus.Dispatched, result.NewStatus);

        var updatedAssignment = await _context.ResourceAssignments.FindAsync(assignment.Id);
        Assert.NotNull(updatedAssignment!.DispatchedAtUtc);

        var history = await _context.ResourceStatusHistories.FirstOrDefaultAsync(x => x.NewStatus == ResourceStatus.Dispatched);
        Assert.NotNull(history);
        Assert.Equal("Dispatched from station", history!.Reason);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenInvalidTransition_ThrowsInvalidOperationException()
    {
        var dept = await CreateDepartmentAsync("Rescue", "RESCUE");
        var resource = await CreateResourceAsync(dept.Id, ResourceStatus.Available, ResourceType.RescueTeam, "Squad 4");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ChangeStatusAsync(resource.Id, ResourceStatus.OnScene, null, null));
    }

    [Fact]
    public async Task ReleaseAsync_WhenValid_CompletesAssignmentAndSetsResourceAvailable()
    {
        var dept = await CreateDepartmentAsync("Police", "POLICE");
        var incident = await CreateIncidentAsync(dept.Id, "Traffic Incident");
        var resource = await CreateResourceAsync(dept.Id, ResourceStatus.Returning, ResourceType.PoliceVehicle, "Patrol 12");

        var assignment = new ResourceAssignment
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            IncidentId = incident.Id,
            AssignedAtUtc = DateTime.UtcNow.AddHours(-1),
            IsActive = true,
            Status = ResourceAssignmentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ResourceAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        await _service.ReleaseAsync(resource.Id, "Scene cleared and returned to base");

        var updatedResource = await _context.Resources.FindAsync(resource.Id);
        Assert.Equal(ResourceStatus.Available, updatedResource!.Status);

        var updatedAssignment = await _context.ResourceAssignments.FindAsync(assignment.Id);
        Assert.False(updatedAssignment!.IsActive);
        Assert.Equal(ResourceAssignmentStatus.Completed, updatedAssignment.Status);
        Assert.NotNull(updatedAssignment.ReleasedAtUtc);
        Assert.Equal("Scene cleared and returned to base", updatedAssignment.ReleaseNotes);

        var audit = await _context.AuditLogs.FirstOrDefaultAsync(x => x.Action == "ResourceReleased");
        Assert.NotNull(audit);
    }

    [Fact]
    public async Task ReleaseAsync_WhenNoActiveAssignment_ThrowsInvalidOperationException()
    {
        var dept = await CreateDepartmentAsync("EMS", "EMS");
        var resource = await CreateResourceAsync(dept.Id, ResourceStatus.Available, ResourceType.Ambulance, "Medic 8");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReleaseAsync(resource.Id, null));
    }

    [Fact]
    public async Task ReleaseAsync_WhenForbiddenTransitionFromCurrentStatus_ThrowsInvalidOperationException()
    {
        var dept = await CreateDepartmentAsync("Fire", "FIRE");
        var incident = await CreateIncidentAsync(dept.Id, "Building Fire");
        var resource = await CreateResourceAsync(dept.Id, ResourceStatus.OnScene, ResourceType.FireTruck, "Engine 2");

        var assignment = new ResourceAssignment
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            IncidentId = incident.Id,
            AssignedAtUtc = DateTime.UtcNow.AddHours(-1),
            IsActive = true,
            Status = ResourceAssignmentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ResourceAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReleaseAsync(resource.Id, "Premature release"));
    }

    [Fact]
    public async Task GetCommandCenterAsync_ReturnsMappedItemsWithActiveIncidents()
    {
        var dept = await CreateDepartmentAsync("Medical", "MED");
        var incident = await CreateIncidentAsync(dept.Id, "Cardiac Arrest");
        var resource = await CreateResourceAsync(dept.Id, ResourceStatus.Assigned, ResourceType.Ambulance, "Medic-1");

        var assignment = new ResourceAssignment
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            IncidentId = incident.Id,
            AssignedAtUtc = DateTime.UtcNow.AddMinutes(-10),
            IsActive = true,
            Status = ResourceAssignmentStatus.Active,
            CreatedAtUtc = DateTime.UtcNow
        };
        _context.ResourceAssignments.Add(assignment);
        await _context.SaveChangesAsync();

        var items = await _service.GetCommandCenterAsync();

        Assert.NotEmpty(items);
        var item = items.Single(x => x.Id == resource.Id);
        Assert.Equal("Medic-1", item.Name);
        Assert.Equal(ResourceType.Ambulance, item.ResourceType);
        Assert.Equal(ResourceStatus.Assigned, item.Status);
        Assert.Equal(dept.Name, item.DepartmentName);
        Assert.Equal(incident.Id, item.ActiveIncidentId);
        Assert.Equal(incident.IncidentNumber, item.ActiveIncidentNumber);
        Assert.Equal(incident.Title, item.ActiveIncidentTitle);
        Assert.NotNull(item.AssignedAtUtc);
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsAccurateStatusCounts()
    {
        var dept = await CreateDepartmentAsync("Police", "POL");
        await CreateResourceAsync(dept.Id, ResourceStatus.Available, ResourceType.PoliceVehicle, "Car-1");
        await CreateResourceAsync(dept.Id, ResourceStatus.Assigned, ResourceType.PoliceVehicle, "Car-2");
        await CreateResourceAsync(dept.Id, ResourceStatus.EnRoute, ResourceType.PoliceVehicle, "Car-3");
        await CreateResourceAsync(dept.Id, ResourceStatus.OnScene, ResourceType.PoliceVehicle, "Car-4");

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(1, summary.Available);
        Assert.Equal(1, summary.Assigned);
        Assert.Equal(1, summary.EnRoute);
        Assert.Equal(1, summary.OnScene);
        Assert.Equal(0, summary.Returning);
        Assert.Equal(0, summary.Maintenance);
    }

    [Fact]
    public async Task GetAssignableIncidentsAsync_ExcludesClosedIncidents()
    {
        var dept = await CreateDepartmentAsync("Fire Dept", "FIRE");
        var openInc = await CreateIncidentAsync(dept.Id, "Structure Fire");
        var closedInc = await CreateIncidentAsync(dept.Id, "Contained Bonfire");
        closedInc.Status = IncidentStatus.Closed;
        await _context.SaveChangesAsync();

        var results = await _service.GetAssignableIncidentsAsync(null);

        Assert.Contains(results, x => x.Id == openInc.Id);
        Assert.DoesNotContain(results, x => x.Id == closedInc.Id);
    }

    [Fact]
    public async Task GetAssignableIncidentsAsync_FiltersByDepartmentForOperator()
    {
        var deptFire = await CreateDepartmentAsync("Fire", "FIR");
        var deptMed = await CreateDepartmentAsync("Medical", "MED2");

        var fireInc = await CreateIncidentAsync(deptFire.Id, "Gas Leak");
        var medInc = await CreateIncidentAsync(deptMed.Id, "Flu Outbreak");

        var fireEngine = await CreateResourceAsync(deptFire.Id, ResourceStatus.Available, ResourceType.FireTruck, "Engine-7");

        _currentUserService.Roles = new List<string> { "Operator" };

        var results = await _service.GetAssignableIncidentsAsync(fireEngine.Id);

        Assert.Contains(results, x => x.Id == fireInc.Id);
        Assert.DoesNotContain(results, x => x.Id == medInc.Id);
    }

    [Fact]
    public async Task GetAssignableIncidentsAsync_AllowsCrossDepartmentForAdmin()
    {
        var deptFire = await CreateDepartmentAsync("Fire Dept 2", "FD2");
        var deptPolice = await CreateDepartmentAsync("Police Dept 2", "PD2");

        var fireInc = await CreateIncidentAsync(deptFire.Id, "Fire Alarm");
        var policeInc = await CreateIncidentAsync(deptPolice.Id, "Traffic Collision");

        var fireEngine = await CreateResourceAsync(deptFire.Id, ResourceStatus.Available, ResourceType.FireTruck, "Engine-9");

        _currentUserService.Roles = new List<string> { "EmergencyAdmin" };

        var results = await _service.GetAssignableIncidentsAsync(fireEngine.Id);

        Assert.Contains(results, x => x.Id == fireInc.Id);
        Assert.Contains(results, x => x.Id == policeInc.Id);
    }

    [Fact]
    public async Task GetByIdAsync_PopulatesStatusHistory()
    {
        var dept = await CreateDepartmentAsync("Search & Rescue", "SAR");
        var resource = await CreateResourceAsync(dept.Id, ResourceStatus.Available, ResourceType.RescueTeam, "K9-Unit-1");

        _context.ResourceStatusHistories.Add(new ResourceStatusHistory
        {
            Id = Guid.NewGuid(),
            ResourceId = resource.Id,
            PreviousStatus = ResourceStatus.Assigned,
            NewStatus = ResourceStatus.Available,
            Reason = "Operation concluded successfully",
            ChangedByUserId = "test-operator",
            ChangedAtUtc = DateTime.UtcNow.AddMinutes(-5),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var details = await _service.GetByIdAsync(resource.Id);

        Assert.NotNull(details);
        Assert.Single(details.StatusHistory);
        var hist = details.StatusHistory[0];
        Assert.Equal(ResourceStatus.Assigned, hist.PreviousStatus);
        Assert.Equal(ResourceStatus.Available, hist.NewStatus);
        Assert.Equal("Operation concluded successfully", hist.Reason);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public string? UserId { get; set; }
        public bool IsAuthenticated => true;
        public List<string> Roles { get; set; } = new();

        public bool IsInRole(string role) => Roles.Contains(role);
    }

    private sealed class FakeIncidentNotificationService : IIncidentNotificationService
    {
        public List<ResourceUpdatedEvent> ResourceEvents { get; } = new();

        public Task NotifyIncidentUpdatedAsync(IncidentUpdatedEvent notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task NotifyDashboardUpdatedAsync(DashboardUpdatedEvent eventData, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task NotifyResourceUpdatedAsync(ResourceUpdatedEvent eventData, CancellationToken cancellationToken = default)
        {
            ResourceEvents.Add(eventData);
            return Task.CompletedTask;
        }
    }

    private sealed class ResourceTestDbContext : ApplicationDbContext
    {
        public ResourceTestDbContext(DbContextOptions<ApplicationDbContext> options)
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
