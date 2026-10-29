using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Persistence.Seed;

public sealed record HostedPortfolioPreview(
    string Dataset, IReadOnlyDictionary<string, int> ExistingCounts,
    IReadOnlyDictionary<string, int> PlannedInserts, bool AlreadySeeded)
{
    public string Fingerprint { get; init; } = "";
    public string TargetServer { get; init; } = "";
    public string TargetDatabase { get; init; } = "";
    public string VerifiedPrincipalSid { get; init; } = "";
}

/// <summary>Explicit synthetic import. Does not register application services or publish jobs.</summary>
public static class HostedPortfolioSeeder
{
    public const string Dataset = "PORTFOLIO24-v1";
    public const string AzureServer = "sql-er-demo-qqgqkekj.database.windows.net";
    public const string Authorization = "SEED SYNTHETIC PORTFOLIO24";
    private static Guid Id(string key) => new(SHA256.HashData(Encoding.UTF8.GetBytes($"{Dataset}/{key}"))[..16]);

    // Target validation is called before opening a connection by the command and
    // again by the write method. SQLite is used only by the isolated unit fixture.
    public static void ValidateConnection(string connection)
    {
        SqlConnectionStringBuilder target;
        try
        {
            if (string.IsNullOrWhiteSpace(connection)) throw new ArgumentException();
            target = new SqlConnectionStringBuilder(connection);
        }
        catch (Exception error) when (error is ArgumentException or FormatException)
        {
            throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
        }
        var azure = target.DataSource.Equals(AzureServer, StringComparison.OrdinalIgnoreCase)
            && target.InitialCatalog == "EmergencyResponse";
        var local = new[] { "localhost", ".", "(localdb)\\MSSQLLocalDB" }
            .Contains(target.DataSource, StringComparer.OrdinalIgnoreCase)
            && target.InitialCatalog == "EmergencyResponse_PortfolioDemo";
        if (!azure && !local) throw new HostedPortfolioException(HostedPortfolioFailure.Target);
        if (azure && (target.Authentication != SqlAuthenticationMethod.ActiveDirectoryDefault
            || target.Encrypt.ToString() == "Optional" || target.TrustServerCertificate || !string.IsNullOrEmpty(target.Password)))
            throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
    }

    private static void ValidateContext(ApplicationDbContext db)
    {
        if (db.Database.IsSqlServer()) ValidateConnection(db.Database.GetConnectionString()!);
        else if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
            throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
        if (db.ChangeTracker.HasChanges()) throw new HostedPortfolioException(HostedPortfolioFailure.Integrity);
    }

    private static readonly (string Type, string City, decimal Lat, decimal Lon, string[] Titles)[] Scenarios =
    [
        ("FLOOD", "Pune", 18.5204m, 73.8567m, ["Underpass waterlogging", "River path inundation", "Market drainage overflow", "Floodwater receded from service lane"]),
        ("FIRE", "Mumbai", 19.0760m, 72.8777m, ["Warehouse smoke report", "Apartment stairwell smoke", "Roadside vegetation fire", "Workshop electrical fire"]),
        ("ROAD_ACCIDENT", "Bengaluru", 12.9716m, 77.5946m, ["Junction collision", "Overturned delivery vehicle", "Bus lane obstruction", "Two vehicle collision"]),
        ("LANDSLIDE", "Rishikesh", 30.0869m, 78.2676m, ["Hillside debris on road", "Rockfall near bend", "Slope movement report", "Minor debris cleared"]),
        ("BUILDING_COLLAPSE", "Delhi", 28.6139m, 77.2090m, ["Boundary wall collapse", "Damaged vacant structure", "Scaffolding collapse report", "Unsafe masonry report"]),
        ("MEDICAL", "Chennai", 13.0827m, 80.2707m, ["Heat exposure exercise", "Public venue medical report", "Cyclist injury report", "Community medical assistance"])
    ];

    private static List<Incident> Build(IReadOnlyDictionary<string, Guid> types, DateTime anchor)
    {
        var rows = new List<Incident>();
        var states = new[] { IncidentStatus.Submitted, IncidentStatus.UnderReview, IncidentStatus.InProgress,
            IncidentStatus.Resolved, IncidentStatus.Closed, IncidentStatus.Rejected };
        for (var i = 0; i < 24; i++)
        {
            var scenario = Scenarios[i / 4];
            var number = $"SYN24-{i + 1:00}";
            var reported = anchor.Date.AddDays(-(i % 14)).AddHours(8 + i % 8);
            var status = states[i % states.Length];
            var row = new Incident
            {
                Id = Id(number), IncidentNumber = number, IncidentTypeId = types[scenario.Type],
                Title = $"[SYNTHETIC] {scenario.Titles[i % 4]}",
                Description = $"Synthetic portfolio exercise {number}; no real emergency. {scenario.Titles[i % 4]} near an invented exercise site in {scenario.City}. Severity, priority and status are authored scenario values, not AI or verified operational findings. Impact counts are unknown.",
                Status = status, Severity = (SeverityLevel)(i % 4 + 1), Priority = (PriorityLevel)(i % 4 + 1),
                ReportedAtUtc = reported, DuplicateDetectionStatus = DuplicateDetectionStatus.NotStarted,
                SeverityReasonsJson = "[\"Authored synthetic exercise value; no inference executed\"]",
                PriorityReasonsJson = "[\"Authored synthetic exercise value; no inference executed\"]"
            };
            row.Locations.Add(new IncidentLocation
            {
                Id = Id($"location/{number}"), IncidentId = row.Id, IsPrimary = true,
                Latitude = scenario.Lat + i % 4 * 0.002m, Longitude = scenario.Lon + i % 4 * 0.002m,
                Address = $"SYNTHETIC exercise site {number}; approximate city coordinates",
                City = scenario.City, Country = "India"
            });
            var path = new List<IncidentStatus> { IncidentStatus.Submitted };
            if (status != IncidentStatus.Submitted) path.Add(IncidentStatus.UnderReview);
            if (status is IncidentStatus.InProgress or IncidentStatus.Resolved or IncidentStatus.Closed)
                path.Add(IncidentStatus.InProgress);
            if (status is IncidentStatus.Resolved or IncidentStatus.Closed) path.Add(IncidentStatus.Resolved);
            if (status is IncidentStatus.Closed or IncidentStatus.Rejected) path.Add(status);
            for (var step = 0; step < path.Count; step++)
                row.StatusHistory.Add(new IncidentStatusHistory
                {
                    Id = Id($"history/{number}/{step}"), IncidentId = row.Id,
                    OldStatus = path[Math.Max(0, step - 1)], NewStatus = path[step],
                    ChangedAtUtc = reported.AddMinutes(step * 10),
                    Remarks = "SYNTHETIC authored history; no dispatch, notification or integration executed."
                });
            rows.Add(row);
        }
        return rows;
    }

    public static void VerifyDatabaseName(string? name, string expected)
    {
        if (name != expected || expected is not ("EmergencyResponse" or "EmergencyResponse_PortfolioDemo"))
            throw new HostedPortfolioException(HostedPortfolioFailure.Target);
    }

    public static async Task<HostedPortfolioPreview> PreviewAsync(ApplicationDbContext db, CancellationToken ct = default,
        HostedPortfolioStages? stages = null, string? expectedPrincipalSid = null)
    {
        stages?.Enter(HostedPortfolioStage.Context);
        ValidateContext(db);
        var principal = "SQLITE-TEST";
        if (db.Database.IsSqlServer())
        {
            var expected = HostedPortfolioSessionVerifier.ValidateExpectedSid(expectedPrincipalSid);
            var session = await HostedPortfolioSessionVerifier.ReadAsync(db, ct);
            HostedPortfolioSessionVerifier.Verify(session, expected);
            principal = session.Sid;
        }
        stages?.Complete();
        stages?.Enter(HostedPortfolioStage.ReferenceData);
        var referenceTypes = await db.IncidentTypes.AsNoTracking().ToListAsync(ct);
        var referenceDepartments = await db.Departments.AsNoTracking().ToListAsync(ct);
        var referenceRoles = await db.Roles.AsNoTracking().ToListAsync(ct);
        var referenceRequirements = await db.IncidentTypeResourceRequirements.AsNoTracking().ToListAsync(ct);
        var types = referenceTypes.Where(x => x.IsActive).ToDictionary(x => x.Code, x => x.Id);
        if (Scenarios.Any(x => !types.ContainsKey(x.Type))) throw new HostedPortfolioException(HostedPortfolioFailure.ReferenceData);
        stages?.Complete();
        stages?.Enter(HostedPortfolioStage.Dataset);
        var rows = Build(types, DateTime.UtcNow.AddDays(-1));
        var ids = rows.Select(x => x.Id).ToArray();
        var numbers = rows.Select(x => x.IncidentNumber).ToArray();
        stages?.Complete();
        stages?.Enter(HostedPortfolioStage.Ownership);
        var marker = await db.AuditLogs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == Id("manifest"), ct);
        var present = await db.Incidents.AsNoTracking().Where(x => ids.Contains(x.Id) || numbers.Contains(x.IncidentNumber)).ToListAsync(ct);
        var owned = marker?.Action == Dataset && marker.EntityId == Dataset;
        if (marker is not null && !owned || !owned && present.Count != 0)
            throw new HostedPortfolioException(HostedPortfolioFailure.Ownership);
        if (owned && (present.Count != 24 || present.Any(x => !rows.Any(expected => expected.Id == x.Id && expected.IncidentNumber == x.IncidentNumber))))
            throw new HostedPortfolioException(HostedPortfolioFailure.Integrity);
        var locationIds = rows.SelectMany(x => x.Locations).Select(x => x.Id).ToArray();
        var historyIds = rows.SelectMany(x => x.StatusHistory).Select(x => x.Id).ToArray();
        // Include both reserved IDs and ANY row attached to reserved parents:
        // this catches stolen IDs, extra rows and incorrectly attached records.
        var locations = await db.IncidentLocations.AsNoTracking()
            .Where(x => locationIds.Contains(x.Id) || ids.Contains(x.IncidentId)).ToListAsync(ct);
        var histories = await db.IncidentStatusHistory.AsNoTracking()
            .Where(x => historyIds.Contains(x.Id) || ids.Contains(x.IncidentId)).ToListAsync(ct);
        var expectedLocations = rows.SelectMany(x => x.Locations).ToDictionary(x => x.Id, x => x.IncidentId);
        var expectedHistories = rows.SelectMany(x => x.StatusHistory).ToDictionary(x => x.Id, x => x.IncidentId);
        if (owned ? locations.Count != 24 || histories.Count != historyIds.Length ||
            locations.Any(x => !expectedLocations.TryGetValue(x.Id, out var parent) || parent != x.IncidentId) ||
            histories.Any(x => !expectedHistories.TryGetValue(x.Id, out var parent) || parent != x.IncidentId)
            : locations.Count != 0 || histories.Count != 0)
            throw new HostedPortfolioException(HostedPortfolioFailure.Integrity);
        stages?.Complete();
        stages?.Enter(HostedPortfolioStage.Inventory);
        var existing = new Dictionary<string, int>
        {
            ["Incidents"] = await db.Incidents.CountAsync(ct), ["Locations"] = await db.IncidentLocations.CountAsync(ct),
            ["Histories"] = await db.IncidentStatusHistory.CountAsync(ct), ["IncidentTypes"] = await db.IncidentTypes.CountAsync(ct),
            ["Departments"] = await db.Departments.CountAsync(ct), ["Users"] = await db.Users.CountAsync(ct),
            ["Roles"] = await db.Roles.CountAsync(ct), ["Resources"] = await db.Resources.CountAsync(ct),
            ["Assignments"] = await db.IncidentAssignments.CountAsync(ct), ["Escalations"] = await db.IncidentEscalations.CountAsync(ct),
            ["Situations"] = await db.OperationalSituations.CountAsync(ct)
        };
        existing["AuditLogs"] = await db.AuditLogs.CountAsync(ct);
        existing["IncidentTypeResourceRequirements"] = referenceRequirements.Count;
        var result = new HostedPortfolioPreview(Dataset, existing, new Dictionary<string, int>
        {
            ["Incidents"] = owned ? 0 : 24, ["Locations"] = owned ? 0 : 24,
            ["Histories"] = owned ? 0 : historyIds.Length, ["OwnershipAudit"] = owned ? 0 : 1
        }, owned);
        var target = db.Database.IsSqlServer() ? new SqlConnectionStringBuilder(db.Database.GetConnectionString()) : null;
        result = result with
        {
            TargetServer = target?.DataSource.ToLowerInvariant() ?? "sqlite-test",
            TargetDatabase = target?.InitialCatalog ?? "isolated-test",
            VerifiedPrincipalSid = target is null ? principal : HostedPortfolioSessionVerifier.ValidateExpectedSid(principal)
        };
        // Canonical ordering, no current time, EF tracking state or dictionary
        // insertion order contributes to the approval. Reference content and
        // timestamps detect replacement/edits even when counts stay identical.
        var canonical = JsonSerializer.Serialize(new
        {
            Version = "HPFP1", result.Dataset, result.TargetServer, result.TargetDatabase, result.VerifiedPrincipalSid,
            result.AlreadySeeded,
            Existing = existing.OrderBy(x => x.Key, StringComparer.Ordinal).ToArray(),
            Planned = result.PlannedInserts.OrderBy(x => x.Key, StringComparer.Ordinal).ToArray(),
            Types = referenceTypes.OrderBy(x => x.Id).Select(x => new { x.Id, x.Code, x.Name, x.Description, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc }).ToArray(),
            Departments = referenceDepartments.OrderBy(x => x.Id).Select(x => new { x.Id, x.Code, x.Name, x.Description, x.IsActive, x.CreatedAtUtc, x.UpdatedAtUtc }).ToArray(),
            Roles = referenceRoles.OrderBy(x => x.Id, StringComparer.Ordinal).Select(x => new { x.Id, x.Name, x.NormalizedName, x.ConcurrencyStamp }).ToArray(),
            Requirements = referenceRequirements.OrderBy(x => x.Id).Select(x => new { x.Id, x.IncidentTypeId, x.ResourceType,
                x.MinimumCount, x.PreferredCount, x.IsMandatory, x.PriorityOrder, x.CreatedAtUtc, x.UpdatedAtUtc }).ToArray(),
            Marker = marker
        });
        result = result with { Fingerprint = "HPFP1:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))) };
        stages?.Complete();
        return result;
    }

    public static Task<HostedPortfolioPreview> ApplyAsync(ApplicationDbContext db, string authorization,
        string expectedFingerprint, string expectedPrincipalSid, CancellationToken ct = default)
        => ApplyCoreAsync(db, authorization, expectedFingerprint, expectedPrincipalSid,
            HostedPortfolioSessionVerifier.ReadAsync, null, ct);

    public static void ValidateFingerprint(string? fingerprint)
    {
        if (fingerprint is null || !Regex.IsMatch(fingerprint, "^HPFP1:[0-9A-F]{64}\\z"))
            throw new HostedPortfolioException(HostedPortfolioFailure.Approval);
    }

    internal static async Task<HostedPortfolioPreview> ApplyCoreAsync(ApplicationDbContext db, string authorization,
        string expectedFingerprint, string expectedPrincipalSid,
        Func<ApplicationDbContext, CancellationToken, Task<HostedPortfolioSession>> readSession,
        Func<Task>? afterSave = null, CancellationToken ct = default)
    {
        if (authorization != Authorization) throw new HostedPortfolioException(HostedPortfolioFailure.Confirmation);
        ValidateFingerprint(expectedFingerprint);
        HostedPortfolioSessionVerifier.ValidateExpectedSid(expectedPrincipalSid);
        ValidateContext(db);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        HostedPortfolioSessionVerifier.Verify(await readSession(db, ct), expectedPrincipalSid);
        var preview = await PreviewAsync(db, ct, expectedPrincipalSid: expectedPrincipalSid);
        if (!string.Equals(preview.Fingerprint, expectedFingerprint, StringComparison.Ordinal))
            throw new HostedPortfolioException(HostedPortfolioFailure.Approval);
        if (!preview.AlreadySeeded)
        {
            var types = await db.IncidentTypes.AsNoTracking().Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, x => x.Id, ct);
            db.Incidents.AddRange(Build(types, DateTime.UtcNow.AddDays(-1)));
            db.AuditLogs.Add(new AuditLog { Id = Id("manifest"), Action = Dataset, EntityName = nameof(HostedPortfolioSeeder),
                EntityId = Dataset, OccurredAtUtc = DateTime.UtcNow, NewValues = "Synthetic dataset v1: 24 incidents; no external effects." });
            await db.SaveChangesAsync(ct);
            if (afterSave is not null) await afterSave(); // Internal isolated-test fault injection only.
        }
        await transaction.CommitAsync(ct);
        return preview;
    }
}
