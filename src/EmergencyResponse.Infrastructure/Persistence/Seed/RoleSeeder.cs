using EmergencyResponse.Application.Common.Roles;

using Microsoft.AspNetCore.Identity;

namespace EmergencyResponse.Infrastructure.Persistence.Seed;

public static class RoleSeeder
{
    public static async Task SeedAsync(
        RoleManager<IdentityRole> roleManager)
    {
        var roles = new[]
        {
            ApplicationRoles.Citizen,
            ApplicationRoles.Responder,
            ApplicationRoles.Operator,
            ApplicationRoles.EmergencyAdmin,
            ApplicationRoles.SystemAdmin
        };

        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(
                    new IdentityRole(roleName));

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        "; ",
                        result.Errors.Select(x => x.Description));

                    throw new InvalidOperationException(
                        $"Unable to create role '{roleName}': {errors}");
                }
            }
        }
    }
}