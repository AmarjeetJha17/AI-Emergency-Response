using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Domain.Notifications;
using EmergencyResponse.Infrastructure.Identity;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using EmergencyResponse.LoadTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.LoadTests.Data;

public sealed record LoadTestDatabaseSnapshot(
    IReadOnlyList<Guid> IncidentIds,
    IReadOnlyList<Guid> SituationIds,
    IReadOnlyList<Guid> ResourceIds,
    IReadOnlyList<Guid> NotificationIds,
    Guid IncidentTypeId,
    string AdminUserId)
{
    public LoadTestDatabaseSnapshot() : this([], [], [], [], Guid.Empty, string.Empty) { }
}

public static class LoadTestDatasetSeeder
{
    private const string FixturePrefix = "LOADTEST-";
    private static readonly DateTime SeededAtUtc = DateTime.UtcNow.AddHours(-1);

    public static async Task<LoadTestDatabaseSnapshot> SeedAsync(
        IServiceProvider services,
        TestDataOptions options,
        string geography,
        string adminEmail,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var users = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var admin = await users.FindByEmailAsync(adminEmail)
            ?? throw new InvalidOperationException("The synthetic load-test administrator was not seeded.");
        await DatabaseSeeder.SeedAsync(db, cancellationToken);

        var existingIncidents = await db.Incidents.AsNoTracking()
            .Where(incident => incident.Title.StartsWith(FixturePrefix))
            .OrderBy(incident => incident.IncidentNumber)
            .Select(incident => incident.Id)
            .ToListAsync(cancellationToken);
        if (existingIncidents.Count > 0)
        {
            if (existingIncidents.Count != options.IncidentCount)
            {
                throw new InvalidOperationException(
                    "The dedicated load-test database contains a different dataset size. Reset only that database before switching dataset profiles.");
            }

            return await ReadSnapshotAsync(db, existingIncidents, admin.Id, cancellationToken);
        }

        var types = await db.IncidentTypes.OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var departments = await db.Departments.OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var incidentTypeIds = types.Select(x => x.Id).ToArray();
        var departmentIds = departments.Select(x => x.Id).ToArray();
        var random = new Random(16_041);
        var incidents = new List<Incident>(options.IncidentCount);
        var locations = new List<IncidentLocation>(options.IncidentCount);
        var histories = new List<IncidentStatusHistory>(options.IncidentCount * options.HistoryPerIncident);

        for (var index = 0; index < options.IncidentCount; index++)
        {
            var id = Guid.NewGuid();
            var reportedAt = SeededAtUtc.AddMinutes(-random.Next(1, 60 * 24 * 30));
            var (latitude, longitude) = CreatePoint(geography, index, random);
            incidents.Add(new Incident
            {
                Id = id,
                CreatedAtUtc = reportedAt,
                ReportedAtUtc = reportedAt,
                IncidentNumber = $"LT-{index:D8}",
                Title = $"{FixturePrefix}Incident {index:D8}",
                Description = "Synthetic emergency response load-test report.",
                IncidentTypeId = incidentTypeIds[index % incidentTypeIds.Length],
                DepartmentId = departmentIds[index % departmentIds.Length],
                ReporterUserId = admin.Id,
                Status = index % 5 == 0 ? IncidentStatus.InProgress : IncidentStatus.UnderReview,
                Severity = (SeverityLevel)(index % 4 + 1),
                Priority = (PriorityLevel)(index % 4 + 1),
                PeopleAffected = index % 35,
                VehiclesAffected = index % 7,
                Casualties = index % 4,
                SeverityScore = index % 101,
                PriorityScore = index % 101
            });
            locations.Add(new IncidentLocation
            {
                Id = Guid.NewGuid(),
                CreatedAtUtc = reportedAt,
                IncidentId = id,
                Latitude = latitude,
                Longitude = longitude,
                Address = $"Synthetic Road {index % 1000}",
                City = "Load Test City",
                District = "Load Test District",
                State = "Load Test State",
                IsPrimary = true
            });
            for (var history = 0; history < options.HistoryPerIncident; history++)
            {
                var timestamp = reportedAt.AddMinutes(history * 4);
                histories.Add(new IncidentStatusHistory
                {
                    Id = Guid.NewGuid(),
                    CreatedAtUtc = timestamp,
                    IncidentId = id,
                    OldStatus = history == 0 ? IncidentStatus.Submitted : IncidentStatus.UnderReview,
                    NewStatus = history == options.HistoryPerIncident - 1 ? incidents[^1].Status : IncidentStatus.UnderReview,
                    ChangedAtUtc = timestamp,
                    ChangedByUserId = admin.Id,
                    Remarks = "Synthetic load-test status history."
                });
            }
        }

        await AddBatchesAsync(db, incidents, cancellationToken);
        await AddBatchesAsync(db, locations, cancellationToken);
        await AddBatchesAsync(db, histories, cancellationToken);

        var incidentIds = incidents.Select(x => x.Id).ToArray();
        var escalations = Enumerable.Range(0, options.EscalationCount).Select(index => new IncidentEscalation
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = SeededAtUtc.AddMinutes(-index),
            IncidentId = incidentIds[index % incidentIds.Length],
            Type = (EscalationType)(index % Enum.GetValues<EscalationType>().Length + 1),
            Level = (EscalationLevel)(index % Enum.GetValues<EscalationLevel>().Length + 1),
            Status = index % 3 == 0 ? EscalationStatus.Acknowledged : EscalationStatus.Open,
            Reason = "Synthetic load-test escalation fixture.",
            EvidenceJson = "[]",
            Score = (index % 100) / 100.0,
            RequiresHumanReview = true
        }).ToList();
        await AddBatchesAsync(db, escalations, cancellationToken);

        var situations = Enumerable.Range(0, options.SituationCount).Select(index => new OperationalSituation
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = SeededAtUtc.AddMinutes(-index),
            SituationNumber = $"LS-{index:D8}",
            Title = $"{FixturePrefix}Situation {index:D8}",
            Summary = "Synthetic command-center situation.",
            Level = (EscalationLevel)(index % Enum.GetValues<EscalationLevel>().Length + 1),
            Status = EscalationStatus.Open,
            SituationScore = index % 101,
            IncidentCount = 1,
            CriticalIncidentCount = index % 5,
            HighIncidentCount = index % 10,
            PeopleAffected = index % 100,
            Casualties = index % 10,
            RequiresHumanReview = true,
            LastEvaluatedAtUtc = SeededAtUtc
        }).ToList();
        await AddBatchesAsync(db, situations, cancellationToken);
        var situationLinks = situations.Select((situation, index) => new OperationalSituationIncident
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = situation.CreatedAtUtc,
            OperationalSituationId = situation.Id,
            IncidentId = incidentIds[index % incidentIds.Length],
            IsPrimaryIncident = true,
            AddedAtUtc = situation.CreatedAtUtc
        }).ToList();
        await AddBatchesAsync(db, situationLinks, cancellationToken);

        var resourceCount = Math.Max(50, (options.ResourceAssignmentCount + 1) / 2);
        var resources = Enumerable.Range(0, resourceCount).Select(index => new Resource
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = SeededAtUtc,
            Name = $"{FixturePrefix}Unit {index:D6}",
            Type = (ResourceType)(index % Enum.GetValues<ResourceType>().Length + 1),
            Status = ResourceStatus.Available,
            DepartmentId = departmentIds[index % departmentIds.Length],
            Latitude = 18.5204m + (index % 100) * 0.001m,
            Longitude = 73.8567m + (index % 100) * 0.001m,
            LocationUpdatedAtUtc = SeededAtUtc,
            Capacity = 4
        }).ToList();
        await AddBatchesAsync(db, resources, cancellationToken);
        var assignments = Enumerable.Range(0, options.ResourceAssignmentCount).Select(index => new ResourceAssignment
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = SeededAtUtc.AddMinutes(-index),
            ResourceId = resources[index % resources.Count].Id,
            IncidentId = incidentIds[index % incidentIds.Length],
            AssignedAtUtc = SeededAtUtc.AddMinutes(-index - 10),
            ReleasedAtUtc = SeededAtUtc.AddMinutes(-index),
            AssignedByUserId = admin.Id,
            IsActive = false,
            Status = ResourceAssignmentStatus.Completed,
            ReleaseNotes = "Synthetic completed assignment."
        }).ToList();
        await AddBatchesAsync(db, assignments, cancellationToken);

        var notifications = Enumerable.Range(0, options.NotificationCount).Select(index => new Notification
        {
            Id = Guid.NewGuid(),
            CreatedAtUtc = SeededAtUtc.AddMinutes(-index),
            UserId = admin.Id,
            Type = NotificationType.System,
            Priority = (NotificationPriority)(index % Enum.GetValues<NotificationPriority>().Length + 1),
            Status = index % 4 == 0 ? NotificationStatus.Read : NotificationStatus.Unread,
            IsRead = index % 4 == 0,
            ReadAtUtc = index % 4 == 0 ? SeededAtUtc : null,
            Title = $"{FixturePrefix}Notification {index:D8}",
            Message = "Synthetic notification load-test data.",
            TargetRole = null,
            DeduplicationKey = $"{FixturePrefix}notification-{index:D8}",
            ExpiresAtUtc = SeededAtUtc.AddDays(30)
        }).ToList();
        await AddBatchesAsync(db, notifications, cancellationToken);

        return new LoadTestDatabaseSnapshot(
            incidentIds.Take(500).ToArray(),
            situations.Select(x => x.Id).Take(500).ToArray(),
            resources.Select(x => x.Id).Take(500).ToArray(),
            notifications.Select(x => x.Id).Take(500).ToArray(),
            incidentTypeIds[0],
            admin.Id);
    }

    private static async Task<LoadTestDatabaseSnapshot> ReadSnapshotAsync(
        ApplicationDbContext db,
        IReadOnlyList<Guid> incidentIds,
        string adminUserId,
        CancellationToken cancellationToken)
    {
        var situations = await db.OperationalSituations.AsNoTracking()
            .Where(item => item.Title.StartsWith(FixturePrefix))
            .OrderBy(item => item.SituationNumber).Select(item => item.Id).Take(500).ToListAsync(cancellationToken);
        var resources = await db.Resources.AsNoTracking()
            .Where(item => item.Name.StartsWith(FixturePrefix))
            .OrderBy(item => item.Name).Select(item => item.Id).Take(500).ToListAsync(cancellationToken);
        var notifications = await db.Notifications.AsNoTracking()
            .Where(item => item.Title.StartsWith(FixturePrefix))
            .OrderBy(item => item.Title).Select(item => item.Id).Take(500).ToListAsync(cancellationToken);
        var incidentTypeId = await db.IncidentTypes.AsNoTracking().OrderBy(item => item.Code)
            .Select(item => item.Id).FirstAsync(cancellationToken);
        return new LoadTestDatabaseSnapshot(incidentIds.Take(500).ToArray(), situations,
            resources, notifications, incidentTypeId, adminUserId);
    }

    private static async Task AddBatchesAsync<T>(ApplicationDbContext db, IReadOnlyList<T> entities, CancellationToken cancellationToken)
        where T : class
    {
        const int batchSize = 500;
        for (var start = 0; start < entities.Count; start += batchSize)
        {
            db.Set<T>().AddRange(entities.Skip(start).Take(batchSize));
            await db.SaveChangesAsync(cancellationToken);
            db.ChangeTracker.Clear();
        }
    }

    private static (decimal Latitude, decimal Longitude) CreatePoint(string distribution, int index, Random random)
    {
        const double centerLat = 18.5204;
        const double centerLon = 73.8567;
        var kind = distribution.ToLowerInvariant();
        if (kind == "uniform")
        {
            return ((decimal)(centerLat + random.NextDouble() * 2 - 1), (decimal)(centerLon + random.NextDouble() * 2 - 1));
        }

        if (kind == "boundary")
        {
            var angle = random.NextDouble() * Math.PI * 2;
            var radiusKm = index % 2 == 0 ? 4.9 : 5.1;
            return ((decimal)(centerLat + Math.Sin(angle) * radiusKm / 111.0),
                (decimal)(centerLon + Math.Cos(angle) * radiusKm / (111.0 * Math.Cos(centerLat * Math.PI / 180))));
        }

        var hotspot = kind != "hotspot" || index % 10 < 7;
        var scale = kind == "hotspot" && !hotspot ? 0.8 : 0.05;
        return ((decimal)(centerLat + (random.NextDouble() - 0.5) * scale),
            (decimal)(centerLon + (random.NextDouble() - 0.5) * scale));
    }
}
