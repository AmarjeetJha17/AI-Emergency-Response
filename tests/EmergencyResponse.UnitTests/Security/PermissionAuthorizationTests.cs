using System.Security.Claims;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.UnitTests.Security;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class PermissionAuthorizationTests
{
    [Fact]
    public async Task PermissionPolicyProvider_CreatesDynamicPolicyForKnownPermission()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync(Permissions.IncidentsRead);

        Assert.NotNull(policy);
        var requirement = Assert.IsType<PermissionRequirement>(Assert.Single(policy.Requirements));
        Assert.Equal(Permissions.IncidentsRead, requirement.Permission);
    }

    [Fact]
    public async Task PermissionPolicyProvider_RecognizesPermissionNamesCaseInsensitively()
    {
        var provider = CreateProvider();

        var policy = await provider.GetPolicyAsync(Permissions.IncidentsRead.ToUpperInvariant());

        Assert.NotNull(policy);
        Assert.Equal(
            Permissions.IncidentsRead,
            Assert.IsType<PermissionRequirement>(Assert.Single(policy.Requirements)).Permission,
            ignoreCase: true);
    }

    [Fact]
    public async Task PermissionPolicyProvider_FallsBackToConfiguredNamedPolicyAndReturnsNullForUnknownPolicy()
    {
        var options = new AuthorizationOptions();
        options.AddPolicy("ExistingNamedPolicy", policy => policy.RequireAuthenticatedUser());
        var provider = new PermissionPolicyProvider(Options.Create(options));

        var fallback = await provider.GetPolicyAsync("ExistingNamedPolicy");
        var unknown = await provider.GetPolicyAsync("NoSuchPolicy");

        Assert.NotNull(fallback);
        Assert.Contains(fallback.Requirements, requirement =>
            requirement is DenyAnonymousAuthorizationRequirement);
        Assert.Null(unknown);
    }

    [Fact]
    public async Task PermissionAuthorizationHandler_GrantsPermissionAssignedToAnyUserRole()
    {
        var requirement = new PermissionRequirement(Permissions.IncidentsRead);
        var context = CreateContext(requirement, ApplicationRoles.Operator);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
        Assert.Empty(context.PendingRequirements);
    }

    [Theory]
    [InlineData(ApplicationRoles.Citizen)]
    [InlineData("UnknownRole")]
    [InlineData(null)]
    public async Task PermissionAuthorizationHandler_DeniesWhenUserHasNoMatchingRole(string? role)
    {
        var requirement = new PermissionRequirement(Permissions.IncidentsRead);
        var context = CreateContext(requirement, role);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        Assert.False(context.HasSucceeded);
        Assert.False(context.HasFailed);
        Assert.Contains(requirement, context.PendingRequirements);
    }

    private static PermissionPolicyProvider CreateProvider()
        => new(Options.Create(new AuthorizationOptions()));

    private static AuthorizationHandlerContext CreateContext(
        PermissionRequirement requirement,
        string? role)
    {
        var claims = role is null
            ? Array.Empty<Claim>()
            : [new Claim(ClaimTypes.Role, role)];
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "unit-test"));
        return new AuthorizationHandlerContext([requirement], user, resource: null);
    }
}
