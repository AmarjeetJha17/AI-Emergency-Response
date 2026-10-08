using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.IntegrationTests.Infrastructure;

public static class TestDatabase
{
    public const string DatabaseName = "EmergencyResponse_IntegrationTests";

    public static async Task InitializeAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(cancellationToken);
    }

    public static async Task ResetAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuredName = db.Database.GetDbConnection().Database;

        if (!string.Equals(configuredName, DatabaseName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to reset database '{configuredName}'. Only '{DatabaseName}' may be reset by integration tests.");
        }

        await db.Database.EnsureDeletedAsync(cancellationToken);
        await db.Database.MigrateAsync(cancellationToken);
    }
}
