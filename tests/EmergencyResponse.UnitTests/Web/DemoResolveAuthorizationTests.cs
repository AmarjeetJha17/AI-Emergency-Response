using System.Reflection;
using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Security.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace EmergencyResponse.UnitTests.Web;

public sealed class DemoResolveAuthorizationTests
{
    [Theory]
    [InlineData(ApplicationRoles.Citizen)]
    [InlineData(ApplicationRoles.Responder)]
    [InlineData(ApplicationRoles.Operator)]
    [InlineData(ApplicationRoles.EmergencyAdmin)]
    [InlineData(ApplicationRoles.SystemAdmin)]
    public void EscalationResolve_WebRoleGuardMatchesExistingPermission(string role)
        => AssertGuard(typeof(EmergencyResponse.Web.Controllers.EscalationsController),
            Permissions.EscalationsResolve, role);

    [Theory]
    [InlineData(ApplicationRoles.Citizen)]
    [InlineData(ApplicationRoles.Responder)]
    [InlineData(ApplicationRoles.Operator)]
    [InlineData(ApplicationRoles.EmergencyAdmin)]
    [InlineData(ApplicationRoles.SystemAdmin)]
    public void SituationResolve_WebRoleGuardMatchesExistingPermission(string role)
        => AssertGuard(typeof(EmergencyResponse.Web.Controllers.SituationsController),
            Permissions.SituationsResolve, role);

    private static void AssertGuard(Type controller, string permission, string role)
    {
        var method = Assert.IsAssignableFrom<MethodInfo>(controller.GetMethod("Resolve"));
        var guard = Assert.Single(method.GetCustomAttributes<AuthorizeAttribute>());
        var allowedRoles = Assert.IsType<string>(guard.Roles).Split(',');
        Assert.Equal(RolePermissions.HasPermission([role], permission), allowedRoles.Contains(role));
        Assert.NotEmpty(method.GetCustomAttributes<Microsoft.AspNetCore.Mvc.ValidateAntiForgeryTokenAttribute>());
    }
}
