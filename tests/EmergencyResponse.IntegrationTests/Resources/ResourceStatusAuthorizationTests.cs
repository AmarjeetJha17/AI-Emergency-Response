using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EmergencyResponse.Application.DTOs.Authentication;
using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Enums;
using EmergencyResponse.Infrastructure.Identity;
using EmergencyResponse.Infrastructure.Persistence.Seed;
using EmergencyResponse.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.IntegrationTests.Resources;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Security)]
[Collection(IntegrationTestCollection.Name)]
public sealed class ResourceStatusAuthorizationTests : IntegrationTestBase
{
    public ResourceStatusAuthorizationTests(IntegrationTestFixture fixture) : base(fixture) { }

    [Theory]
    [InlineData("Operator", HttpStatusCode.Forbidden)]
    [InlineData("EmergencyAdmin", HttpStatusCode.NotFound)]
    public async Task GenericStatusEndpoint_UsesDispatchPermissionBeforeResourceLookup(string role, HttpStatusCode expected)
    {
        var email = $"resource-policy-{Guid.NewGuid():N}@test.local";
        const string password = "Test@123!";
        await using (var scope = Factory.Services.CreateAsyncScope())
        {
            await TestUsersSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                Options.Create(new TestUsersSeedOptions { Operator = new TestUserSeedEntry {
                    Email = email, Password = password, Role = role, FirstName = "Test", LastName = "Resource policy"
                }}), NullLogger.Instance);
        }
        using var login = await Client.PostAsJsonAsync("/api/auth/login", new LoginRequest { Email = email, Password = password });
        await login.EnsureSuccessWithBodyAsync();
        var auth = await login.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(auth);
        Assert.Contains(role, auth.Roles);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // A nonexistent target proves denied requests never reach the lookup or mutation.
        using var response = await Client.PostAsJsonAsync($"/api/resources/{Guid.NewGuid()}/status",
            new ChangeResourceStatusRequest { Status = ResourceStatus.Dispatched, Reason = "Policy regression" });
        Assert.Equal(expected, response.StatusCode);
    }
}
