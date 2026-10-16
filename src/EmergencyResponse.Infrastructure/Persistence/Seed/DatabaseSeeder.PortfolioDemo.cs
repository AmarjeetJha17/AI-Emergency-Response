using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Security.Authorization;
using EmergencyResponse.Domain.Common;
using EmergencyResponse.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace EmergencyResponse.Infrastructure.Persistence.Seed;

public static partial class DatabaseSeeder
{
    private sealed record DemoOwnershipManifest(
        string Dataset, string ReporterUserId, string OperatorUserId, Dictionary<string, Guid[]> Records);

    public static async Task<PortfolioDemoSnapshot> SeedPortfolioDemoAsync(
        ApplicationDbContext context, PortfolioDemoSeedOptions options,
        CancellationToken cancellationToken = default)
    {
        ValidatePortfolioDemoTarget(context, options.EnvironmentName);
        RequireCleanUnitOfWork(context);
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        await ValidateDemoActorsAsync(context, options, cancellationToken);
        // Reuse the existing reference-data seeder, never create parallel types/departments.
        await SeedAsync(context, cancellationToken);
        var types = await context.IncidentTypes.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken);
        var departments = await context.Departments.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code, x => x.Id, cancellationToken);
        foreach (var code in new[] { "FLOOD", "FIRE", "ROAD_ACCIDENT", "BUILDING_COLLAPSE" })
            if (!types.ContainsKey(code)) throw new InvalidOperationException($"Required active incident type missing: {code}.");
        foreach (var code in new[] { "DISASTER", "FIRE", "POLICE", "MEDICAL", "PWD" })
            if (!departments.ContainsKey(code)) throw new InvalidOperationException($"Required active department missing: {code}.");
        var records = PortfolioDemoDataset.Build(types, departments, options.ReporterUserId, options.OperatorUserId, DateTime.UtcNow);
        var expected = RecordIds(records);
        var marker = await context.AuditLogs.SingleOrDefaultAsync(x => x.Id == PortfolioDemoDataset.ManifestId, cancellationToken);
        if (marker is not null)
        {
            var manifest = ReadManifest(marker, expected);
            if (manifest.ReporterUserId != options.ReporterUserId || manifest.OperatorUserId != options.OperatorUserId)
                throw new InvalidOperationException("Demo already belongs to different actors; cleanup or use those actors. No records were overwritten.");
            foreach (var group in expected)
            {
                if ((await LoadRecordsAsync(context, group.Key, group.Value, cancellationToken)).Count != group.Value.Length)
                    throw new InvalidOperationException("Demo is incomplete. Review and clean the owned dataset before reseeding.");
            }
            await transaction.CommitAsync(cancellationToken);
            return Snapshot(expected, false);
        }
        foreach (var group in expected)
            if ((await LoadRecordsAsync(context, group.Key, group.Value, cancellationToken)).Count != 0)
                throw new InvalidOperationException("Reserved demo identifiers exist without ownership evidence. Refusing to adopt or overwrite them.");
        // Identifiers are unique in the existing model; explicitly fail on a
        // foreign row with the same visible number rather than adopting it.
        var numbers = records.OfType<Incident>().Select(x => x.IncidentNumber).ToArray();
        if (await context.Incidents.AnyAsync(x => numbers.Contains(x.IncidentNumber), cancellationToken))
            throw new InvalidOperationException("Demo incident number collision. No records were adopted.");
        context.AddRange(records);
        context.AuditLogs.Add(new AuditLog
        {
            Id = PortfolioDemoDataset.ManifestId, UserId = options.OperatorUserId,
            Action = PortfolioDemoDataset.OwnerAction, EntityName = nameof(DatabaseSeeder), EntityId = PortfolioDemoDataset.Key,
            OccurredAtUtc = DateTime.UtcNow,
            NewValues = JsonSerializer.Serialize(new DemoOwnershipManifest(PortfolioDemoDataset.Key, options.ReporterUserId, options.OperatorUserId, expected))
        });
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Snapshot(expected, true);
    }

    public static async Task<PortfolioDemoCleanupResult> CleanupPortfolioDemoAsync(
        ApplicationDbContext context, string environmentName, bool apply = false,
        CancellationToken cancellationToken = default)
    {
        ValidatePortfolioDemoTarget(context, environmentName);
        RequireCleanUnitOfWork(context);
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var marker = await context.AuditLogs.SingleOrDefaultAsync(x => x.Id == PortfolioDemoDataset.ManifestId, cancellationToken);
        if (marker is null) return new(false, new Dictionary<string, int>());
        // Definition IDs do not depend on reference IDs, actors or dates.
        var placeholderTypes = new[] { "FLOOD", "FIRE", "ROAD_ACCIDENT", "BUILDING_COLLAPSE" }.ToDictionary(x => x, _ => Guid.Empty);
        var placeholderDepartments = new[] { "DISASTER", "FIRE", "POLICE", "MEDICAL", "PWD" }.ToDictionary(x => x, _ => Guid.Empty);
        var expected = RecordIds(PortfolioDemoDataset.Build(placeholderTypes, placeholderDepartments, "", "", DateTime.UtcNow));
        var manifest = ReadManifest(marker, expected);
        var situationIds = manifest.Records[nameof(OperationalSituation)];
        var situationAnalyses = await context.OperationalSituationAnalyses
            .Where(x => situationIds.Contains(x.OperationalSituationId))
            .ToListAsync(cancellationToken);
        if (situationAnalyses.Any(x => x.PortfolioDemoOwnershipKey != PortfolioDemoDataset.Key))
            throw new InvalidOperationException($"Cleanup blocked: {nameof(OperationalSituationAnalysis)} contains dependent records not owned by DEMO18.");

        var guardedIds = new Dictionary<string, Guid[]>(manifest.Records)
        {
            [nameof(AuditLog)] = [PortfolioDemoDataset.ManifestId],
            [nameof(OperationalSituationAnalysis)] = situationAnalyses.Select(x => x.Id).ToArray()
        };
        await RefuseForeignReferencesAsync(context, guardedIds, cancellationToken);
        var owned = new List<BaseEntity>();
        var counts = new Dictionary<string, int>();
        foreach (var group in manifest.Records)
        {
            var rows = await LoadRecordsAsync(context, group.Key, group.Value, cancellationToken);
            counts[group.Key] = rows.Count;
            owned.AddRange(rows);
        }
        owned.AddRange(situationAnalyses);
        counts[nameof(OperationalSituationAnalysis)] = situationAnalyses.Count;
        counts[nameof(AuditLog)] = 1;
        if (apply)
        {
            // The definition lists principals before dependents. Mark dependents
            // first so required Restrict links are not severed by EF fix-up.
            context.RemoveRange(owned.AsEnumerable().Reverse());
            context.AuditLogs.Remove(marker);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        return new(apply, counts);
    }

    public static void ValidatePortfolioDemoTarget(ApplicationDbContext context, string environment)
    {
        if (environment is not ("Development" or "Test"))
            throw new InvalidOperationException("Demo import/cleanup is restricted to Development or the isolated Test host.");
        if (context.Database.ProviderName?.EndsWith("Sqlite", StringComparison.Ordinal) == true)
        {
            var sqlite = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = context.Database.GetDbConnection().ConnectionString };
            if (environment == "Test" && sqlite.TryGetValue("Data Source", out var source) && Equals(source, ":memory:")) return;
            throw new InvalidOperationException("SQLite demo operations are restricted to in-memory tests.");
        }
        var connection = new SqlConnectionStringBuilder(context.Database.GetConnectionString());
        var server = connection.DataSource.Split(',')[0].Split('\\')[0];
        var database = environment == "Test" ? "EmergencyResponse_IntegrationTests" : "EmergencyResponse_PortfolioDemo";
        if (server is not ("localhost" or "127.0.0.1" or ".") || connection.InitialCatalog != database)
            throw new InvalidOperationException("Demo operation refused: use the exact isolated demo/test database on local SQL Server.");
    }

    private static void RequireCleanUnitOfWork(ApplicationDbContext context)
    {
        if (context.ChangeTracker.HasChanges())
            throw new InvalidOperationException("Use a clean DbContext; demo operations must not commit unrelated changes.");
    }

    private static async Task ValidateDemoActorsAsync(ApplicationDbContext db, PortfolioDemoSeedOptions options, CancellationToken token)
    {
        foreach (var id in new[] { options.ReporterUserId, options.OperatorUserId })
            if (string.IsNullOrWhiteSpace(id) || !await db.Users.AnyAsync(x => x.Id == id && x.IsActive, token))
                throw new InvalidOperationException("Demo actors must be existing active accounts. Accounts are not created or elevated by the importer.");
        var roles = await (from userRole in db.UserRoles join role in db.Roles on userRole.RoleId equals role.Id
                           where userRole.UserId == options.OperatorUserId select role.Name!).ToListAsync(token);
        if (!RolePermissions.HasPermission(roles, Permissions.IncidentsRead) || !RolePermissions.HasPermission(roles, Permissions.SituationsRead))
            throw new InvalidOperationException("The demo review account must have existing incident and situation read permissions.");
        var isCitizen = await (from userRole in db.UserRoles join role in db.Roles on userRole.RoleId equals role.Id
                               where userRole.UserId == options.ReporterUserId && role.Name == ApplicationRoles.Citizen select userRole).AnyAsync(token);
        if (!isCitizen) throw new InvalidOperationException("The demo reporter must be an existing Citizen.");
    }

    private static Dictionary<string, Guid[]> RecordIds(IEnumerable<BaseEntity> records) =>
        records.GroupBy(x => x.GetType().Name).ToDictionary(x => x.Key, x => x.Select(y => y.Id).Order().ToArray());

    private static DemoOwnershipManifest ReadManifest(AuditLog marker, Dictionary<string, Guid[]> expected)
    {
        if (marker.Action != PortfolioDemoDataset.OwnerAction || marker.EntityName != nameof(DatabaseSeeder) || marker.EntityId != PortfolioDemoDataset.Key)
            throw new InvalidOperationException("Demo ownership marker does not match. Refusing cleanup/import.");
        DemoOwnershipManifest? manifest;
        try { manifest = JsonSerializer.Deserialize<DemoOwnershipManifest>(marker.NewValues ?? ""); }
        catch (JsonException) { throw new InvalidOperationException("Demo ownership manifest is invalid. No records were changed."); }
        if (manifest?.Dataset != PortfolioDemoDataset.Key || manifest.Records is null || manifest.Records.Count != expected.Count ||
            expected.Any(pair => !manifest.Records.TryGetValue(pair.Key, out var ids) || ids is null || !ids.Order().SequenceEqual(pair.Value)))
            throw new InvalidOperationException("Demo ownership manifest contains unexpected identifiers. No records were changed.");
        return manifest;
    }

    private static PortfolioDemoSnapshot Snapshot(Dictionary<string, Guid[]> ids, bool created) =>
        new(created, ids[nameof(Incident)], ids[nameof(Resource)], ids[nameof(IncidentEscalation)],
            ids[nameof(OperationalSituation)], ids[nameof(Notification)]);

    private static Task<List<BaseEntity>> LoadRecordsAsync(ApplicationDbContext db, string entityName, Guid[] ids, CancellationToken token)
    {
        var type = db.Model.GetEntityTypes().Single(x => x.ClrType.Name == entityName).ClrType;
        return (Task<List<BaseEntity>>)typeof(DatabaseSeeder).GetMethod(nameof(LoadTypedRecordsAsync), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(type).Invoke(null, [db, ids, token])!;
    }

    private static async Task<List<BaseEntity>> LoadTypedRecordsAsync<T>(ApplicationDbContext db, Guid[] ids, CancellationToken token) where T : BaseEntity =>
        (await db.Set<T>().Where(x => ids.Contains(x.Id)).ToListAsync(token)).Cast<BaseEntity>().ToList();

    private static async Task RefuseForeignReferencesAsync(ApplicationDbContext db, Dictionary<string, Guid[]> owned, CancellationToken token)
    {
        // Inspect every mapped FK, including cascading and SET NULL relationships.
        // A new application entity cannot silently become collateral cleanup.
        foreach (var entity in db.Model.GetEntityTypes())
        foreach (var fk in entity.GetForeignKeys())
        {
            if (!owned.TryGetValue(fk.PrincipalEntityType.ClrType.Name, out var principalIds)) continue;
            if (fk.Properties.Count != 1 || !typeof(BaseEntity).IsAssignableFrom(entity.ClrType))
                throw new InvalidOperationException("Cleanup cannot safely evaluate an additional relationship; review required.");
            var dependentIds = owned.GetValueOrDefault(entity.ClrType.Name) ?? [];
            var check = (Task<bool>)typeof(DatabaseSeeder).GetMethod(nameof(HasForeignReferencesAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entity.ClrType).Invoke(null, [db, fk.Properties[0].Name, principalIds, dependentIds, token])!;
            if (await check) throw new InvalidOperationException($"Cleanup blocked: {entity.ClrType.Name} contains dependent records not owned by DEMO18.");
        }
        var allIds = owned.Values.SelectMany(x => x).ToArray();
        // Several existing models (notes, tags and job work items) carry ID
        // references without an EF relationship. Protect those records too.
        foreach (var entity in db.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties().Where(x => x.Name != nameof(BaseEntity.Id) &&
                     (x.ClrType == typeof(Guid) || x.ClrType == typeof(Guid?)) && !x.GetContainingForeignKeys().Any()))
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entity.ClrType))
                throw new InvalidOperationException("Cleanup cannot safely evaluate an additional ID reference; review required.");
            var check = (Task<bool>)typeof(DatabaseSeeder).GetMethod(nameof(HasForeignReferencesAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(entity.ClrType).Invoke(null, [db, property.Name, allIds, owned.GetValueOrDefault(entity.ClrType.Name) ?? [], token])!;
            if (await check) throw new InvalidOperationException($"Cleanup blocked: {entity.ClrType.Name} references DEMO18 records but is not owned by the importer.");
        }
        var textIds = allIds.Select(x => x.ToString()).ToArray();
        if (await db.AuditLogs.AnyAsync(x => textIds.Contains(x.EntityId), token))
            throw new InvalidOperationException("Cleanup blocked: additional audit evidence references demo records.");
        foreach (var id in textIds)
            if (await db.OutboxMessages.AnyAsync(x => x.PayloadJson.Contains(id), token))
                throw new InvalidOperationException("Cleanup blocked: durable messages reference demo records.");
    }

    private static Task<bool> HasForeignReferencesAsync<T>(ApplicationDbContext db, string propertyName, Guid[] principalIds, Guid[] dependentIds, CancellationToken token) where T : BaseEntity
    {
        var parameter = Expression.Parameter(typeof(T), "row");
        var foreignKey = Expression.Property(parameter, propertyName);
        Expression matches;
        var contains = typeof(Enumerable).GetMethods().Single(x => x.Name == nameof(Enumerable.Contains) && x.GetParameters().Length == 2).MakeGenericMethod(typeof(Guid));
        if (foreignKey.Type == typeof(Guid?))
            matches = Expression.AndAlso(Expression.Property(foreignKey, "HasValue"),
                Expression.Call(contains, Expression.Constant(principalIds), Expression.Property(foreignKey, "Value")));
        else
            matches = Expression.Call(contains, Expression.Constant(principalIds), foreignKey);
        var isOwned = Expression.Call(contains, Expression.Constant(dependentIds), Expression.Property(parameter, nameof(BaseEntity.Id)));
        var predicate = Expression.Lambda<Func<T, bool>>(Expression.AndAlso(matches, Expression.Not(isOwned)), parameter);
        return db.Set<T>().AnyAsync(predicate, token);
    }
}
