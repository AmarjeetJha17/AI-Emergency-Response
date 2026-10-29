using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EmergencyResponse.Infrastructure.Persistence.Seed;

internal sealed record HostedPortfolioSession(string Sid, string PrincipalType, bool OriginalContext, bool PermissionsVerified);

internal static class HostedPortfolioSessionVerifier
{
    internal static string ValidateExpectedSid(string? sid)
    {
        if (sid is null || !Regex.IsMatch(sid, "^0x(?:[0-9A-Fa-f]{32}|[0-9A-Fa-f]{36})\\z"))
            throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
        return "0x" + sid[2..].ToUpperInvariant();
    }

    internal static void Verify(HostedPortfolioSession session, string expected)
    {
        expected = ValidateExpectedSid(expected);
        if (!string.Equals(session.Sid, expected, StringComparison.OrdinalIgnoreCase) ||
            session.PrincipalType != "E" || !session.OriginalContext)
            throw new HostedPortfolioException(HostedPortfolioFailure.Principal);
        if (!session.PermissionsVerified)
            throw new HostedPortfolioException(HostedPortfolioFailure.Permissions);
    }

    internal static async Task<HostedPortfolioSession> ReadAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (!db.Database.IsSqlServer()) throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
        var connection = db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await db.Database.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        // Require a direct external database user, not dbo, guest, a group, or
        // an impersonated context. Group-only sessions fail closed deliberately.
        command.CommandText = """
            SELECT CONVERT(varchar(170), sid, 1), type,
              CASE WHEN ORIGINAL_LOGIN() = SUSER_SNAME() AND ORIGINAL_LOGIN() = name
                AND authentication_type_desc = 'EXTERNAL' THEN 1 ELSE 0 END,
              DB_NAME()
            FROM sys.database_principals WHERE principal_id = DATABASE_PRINCIPAL_ID()
            """;
        string sid;
        string type;
        bool original;
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct) || reader.IsDBNull(0))
                throw new HostedPortfolioException(HostedPortfolioFailure.Principal);
            sid = reader.GetString(0);
            type = reader.GetString(1);
            original = reader.GetInt32(2) == 1;
            HostedPortfolioSeeder.VerifyDatabaseName(reader.GetString(3),
                new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(db.Database.GetConnectionString()).InitialCatalog);
        }
        var permissions = true;
        foreach (var table in new[] { "Incidents", "IncidentLocations", "IncidentStatusHistory", "AuditLogs",
                     "IncidentTypes", "IncidentTypeResourceRequirements", "Departments", "AspNetUsers", "AspNetRoles", "Resources", "IncidentAssignments", "IncidentEscalations", "OperationalSituations" })
        {
            foreach (var permission in new[] { "SELECT", "INSERT" })
            {
                if (permission == "INSERT" && table is not ("Incidents" or "IncidentLocations" or "IncidentStatusHistory" or "AuditLogs")) continue;
                command.Parameters.Clear();
                var objectParameter = command.CreateParameter();
                objectParameter.ParameterName = "@object"; objectParameter.Value = "dbo." + table;
                var permissionParameter = command.CreateParameter();
                permissionParameter.ParameterName = "@permission"; permissionParameter.Value = permission;
                command.Parameters.Add(objectParameter); command.Parameters.Add(permissionParameter);
                command.CommandText = "SELECT HAS_PERMS_BY_NAME(@object, 'OBJECT', @permission)";
                permissions &= await command.ExecuteScalarAsync(ct) is int value && value == 1;
            }
        }
        return new(sid, type, original, permissions);
    }
}
