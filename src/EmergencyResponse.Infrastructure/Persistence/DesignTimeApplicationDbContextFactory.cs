using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmergencyResponse.Infrastructure.Persistence;

/// <summary>
/// Creates the EF context for migrations without starting the API host or its seeders.
/// </summary>
public sealed class DesignTimeApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__TestDatabase") ??
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ??
            "Server=localhost;Database=EmergencyResponse_DesignTime;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}
