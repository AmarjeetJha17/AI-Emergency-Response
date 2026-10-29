using System.Text.Json;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

internal static class HostedPortfolioCommand
{
    internal static async Task<int> RunAsync(string[] args)
    {
        // Intentionally do not build an application host or register workers.
        var stages = new HostedPortfolioStages(Console.Error.WriteLine);
        try
        {
            stages.Enter(HostedPortfolioStage.Configuration);
            var approval = HostedPortfolioApprovalInput.Parse(args);
            var connection = Environment.GetEnvironmentVariable("PORTFOLIO_SEED_CONNECTION");
            if (string.IsNullOrWhiteSpace(connection)) throw new HostedPortfolioException(HostedPortfolioFailure.Configuration);
            HostedPortfolioSeeder.ValidateConnection(connection);
            stages.Complete();
            stages.Enter(HostedPortfolioStage.Context);
            await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer(connection).Options);
            _ = db.Model; // Force EF model initialization inside the reported stage.
            stages.Complete();
            stages.Enter(HostedPortfolioStage.OpenConnection);
            await db.Database.OpenConnectionAsync();
            stages.Complete();
            stages.Enter(HostedPortfolioStage.VerifyTarget);
            await using (var target = db.Database.GetDbConnection().CreateCommand())
            {
                target.CommandText = "SELECT DB_NAME()";
                var actualDatabase = await target.ExecuteScalarAsync();
                HostedPortfolioSeeder.VerifyDatabaseName(actualDatabase as string,
                    new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog);
            }
            stages.Complete();
            var preview = await HostedPortfolioSeeder.PreviewAsync(db, stages: stages,
                expectedPrincipalSid: approval.ExpectedPrincipalSid);
            stages.Enter(HostedPortfolioStage.Serialization);
            var output = JsonSerializer.Serialize(preview, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine(output);
            stages.Complete();
            if (args[0] == "hosted-preview") return 0;
            if (preview.Fingerprint != approval.ExpectedFingerprint)
                throw new HostedPortfolioException(HostedPortfolioFailure.Approval);
            // Write mode always requires a separate interactive authorization.
            // No command argument or environment variable can bypass the prompt.
            stages.Enter(HostedPortfolioStage.Confirmation);
            if (Console.IsInputRedirected) throw new HostedPortfolioException(HostedPortfolioFailure.Confirmation);
            Console.WriteLine($"Type {HostedPortfolioSeeder.Authorization} to authorize writes to the configured synthetic target:");
            if (Console.ReadLine() != HostedPortfolioSeeder.Authorization) throw new HostedPortfolioException(HostedPortfolioFailure.Confirmation);
            stages.Complete();
            stages.Enter(HostedPortfolioStage.Write);
            await HostedPortfolioSeeder.ApplyAsync(db, HostedPortfolioSeeder.Authorization,
                approval.ExpectedFingerprint!, approval.ExpectedPrincipalSid);
            stages.Complete();
            Console.WriteLine("Synthetic import completed. Existing rows were not overwritten.");
            return 0;
        }
        catch (Exception error)
        {
            // Connection exceptions can contain credentials/server response data.
            Console.Error.WriteLine(stages.Failure(error));
            return 1;
        }
    }
}
