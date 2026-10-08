using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using EmergencyResponse.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.IntegrationTests.Infrastructure;

public static class TestDataSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        await DatabaseSeeder.SeedAsync(
            provider.GetRequiredService<ApplicationDbContext>(), cancellationToken);
        await RoleSeeder.SeedAsync(provider.GetRequiredService<RoleManager<IdentityRole>>());
        await AdminSeeder.SeedAsync(
            provider.GetRequiredService<UserManager<ApplicationUser>>(),
            provider.GetRequiredService<IOptions<AdminSeedOptions>>());
    }
}
