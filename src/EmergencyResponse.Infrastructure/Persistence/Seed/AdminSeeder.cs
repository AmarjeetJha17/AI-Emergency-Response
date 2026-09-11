using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Infrastructure.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.Persistence.Seed;

public static class AdminSeeder
{
    public static async Task SeedAsync(
        UserManager<ApplicationUser> userManager,
        IOptions<AdminSeedOptions> options)
    {
        var settings = options.Value;

        var existingUser =
            await userManager.FindByEmailAsync(settings.Email);

        if (existingUser is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = settings.Email,
            Email = settings.Email,
            EmailConfirmed = true,
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(
            user,
            settings.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(x => x.Description));

            throw new InvalidOperationException(
                $"Unable to create seed administrator: {errors}");
        }

        var roleResult = await userManager.AddToRoleAsync(
            user,
            ApplicationRoles.SystemAdmin);

        if (!roleResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                roleResult.Errors.Select(x => x.Description));

            throw new InvalidOperationException(
                $"Unable to assign administrator role: {errors}");
        }
    }
}