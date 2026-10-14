using System.Text.Json;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

if (args.Length == 0 || args[0] is not ("seed" or "cleanup"))
{
    Console.Error.WriteLine("Usage: DemoData seed --reporter-email <existing-citizen> --operator-email <existing-reviewer> | cleanup [--apply]");
    return 2;
}
try
{
    var settings = new Dictionary<string, string>(StringComparer.Ordinal);
    var apply = false;
    for (var index = 1; index < args.Length; index++)
    {
        if (args[index] == "--apply" && args[0] == "cleanup") { apply = true; continue; }
        if (args[0] != "seed" || args[index] is not ("--reporter-email" or "--operator-email") || index + 1 == args.Length)
            throw new InvalidOperationException("Unknown or incomplete demo argument.");
        settings.Add(args[index], args[++index]);
    }
    var builder = Host.CreateApplicationBuilder();
    if (builder.Environment.EnvironmentName != "Development")
        throw new InvalidOperationException("Set DOTNET_ENVIRONMENT=Development. This tool cannot operate in production, staging, or Test.");
    var connection = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Configure ConnectionStrings:DefaultConnection in API User Secrets or environment variables.");
    await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
    // Both shared methods validate exact local database/server before any I/O.
    object result;
    if (args[0] == "cleanup")
        result = await DatabaseSeeder.CleanupPortfolioDemoAsync(db, builder.Environment.EnvironmentName, apply);
    else
    {
        DatabaseSeeder.ValidatePortfolioDemoTarget(db, builder.Environment.EnvironmentName);
        if (!settings.TryGetValue("--reporter-email", out var reporterEmail) || !settings.TryGetValue("--operator-email", out var operatorEmail))
            throw new InvalidOperationException("Existing reporter and review account emails are required.");
        var reporter = await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == reporterEmail.ToUpperInvariant())
            ?? throw new InvalidOperationException("Reporter account not found; register a synthetic Citizen through the existing application.");
        var reviewer = await db.Users.SingleOrDefaultAsync(x => x.NormalizedEmail == operatorEmail.ToUpperInvariant())
            ?? throw new InvalidOperationException("Review account not found; use an existing authorized Operator/admin.");
        result = await DatabaseSeeder.SeedPortfolioDemoAsync(db, new("Development", reporter.Id, reviewer.Id));
    }
    Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
catch (Exception error) when (error is InvalidOperationException or ArgumentException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
