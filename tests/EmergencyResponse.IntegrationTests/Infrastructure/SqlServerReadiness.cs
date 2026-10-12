using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace EmergencyResponse.IntegrationTests.Infrastructure;

public static class SqlServerReadiness
{
    private const int MaximumAttempts = 15;

    public static async Task WaitForSqlServerAsync()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.Test.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var configuredConnectionString =
            configuration.GetConnectionString("TestDatabase");
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:TestDatabase must be configured for SQL integration tests.");
        }

        var connectionBuilder = new SqlConnectionStringBuilder(configuredConnectionString);
        if (!string.Equals(
                connectionBuilder.InitialCatalog,
                "EmergencyResponse_IntegrationTests",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to run integration tests against database '{connectionBuilder.InitialCatalog}'.");
        }

        connectionBuilder.InitialCatalog = "master";
        connectionBuilder.ConnectTimeout = 5;
        var connectionString = connectionBuilder.ConnectionString;

        SqlException? lastFailure = null;
        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync();
                return;
            }
            catch (SqlException exception)
            {
                lastFailure = exception;
                if (attempt < MaximumAttempts)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
        }

        throw new InvalidOperationException(
            $"SQL Server did not become ready after {MaximumAttempts} attempts.",
            lastFailure);
    }
}
