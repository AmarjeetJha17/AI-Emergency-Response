using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Data.SqlClient;

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

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        var accessToken = Environment.GetEnvironmentVariable("PORTFOLIO_SQL_ACCESS_TOKEN");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
        else
        {
            var connection = new SqlConnection(connectionString)
            {
                AccessToken = accessToken
            };
            optionsBuilder.UseSqlServer(connection);
        }

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
