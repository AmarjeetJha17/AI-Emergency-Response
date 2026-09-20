using EmergencyResponse.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds development test users (Operator, EmergencyAdmin).
/// Runs only when TestUsersSeed config section is present and populated.
/// Safe to run on every startup — skips users that already exist.
/// </summary>
public static class TestUsersSeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        IOptions<TestUsersSeedOptions> options,
        ILogger logger)
    {
        var settings = options.Value;

        await SeedUserAsync(
            userManager,
            settings.Operator,
            logger);

        await SeedUserAsync(
            userManager,
            settings.EmergencyAdmin,
            logger);
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        TestUserSeedEntry entry,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(entry.Email) ||
            string.IsNullOrWhiteSpace(entry.Password) ||
            string.IsNullOrWhiteSpace(entry.Role))
        {
            // Entry not configured — skip silently.
            return;
        }

        var existing = await userManager.FindByEmailAsync(entry.Email);

        if (existing is not null)
        {
            logger.LogDebug(
                "Test user {Email} already exists — skipping.",
                entry.Email);

            return;
        }

        var user = new ApplicationUser
        {
            UserName = entry.Email,
            Email = entry.Email,
            EmailConfirmed = true,
            FirstName = entry.FirstName,
            LastName = entry.LastName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(
            user,
            entry.Password);

        if (!createResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                createResult.Errors.Select(e => e.Description));

            throw new InvalidOperationException(
                $"Unable to create test user '{entry.Email}': {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(
            user,
            entry.Role);

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                roleResult.Errors.Select(e => e.Description));

            throw new InvalidOperationException(
                $"Unable to assign role '{entry.Role}' to '{entry.Email}': {errors}");
        }

        logger.LogInformation(
            "Seeded test user {Email} with role {Role}.",
            entry.Email,
            entry.Role);
    }
}
