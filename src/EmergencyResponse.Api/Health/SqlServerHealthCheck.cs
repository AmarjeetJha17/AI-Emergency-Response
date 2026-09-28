using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EmergencyResponse.Api.Health;

public sealed class SqlServerHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _db;

    public SqlServerHealthCheck(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connected = await _db.Database.CanConnectAsync(cancellationToken);
            return connected
                ? HealthCheckResult.Healthy("SQL Server is reachable.")
                : HealthCheckResult.Unhealthy("SQL Server is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server health check failed.", ex);
        }
    }
}
