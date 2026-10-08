using EmergencyResponse.Application.Common.Roles;
using EmergencyResponse.Application.Security.Authorization;

namespace EmergencyResponse.UnitTests.Security;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Security)]
public sealed class RolePermissionMatrixTests
{
    [Theory]
    [InlineData(ApplicationRoles.Citizen, Permissions.IncidentsCreate, true)]
    [InlineData(ApplicationRoles.Citizen, Permissions.IncidentsRead, false)]
    [InlineData(ApplicationRoles.Responder, Permissions.IncidentsRead, true)]
    [InlineData(ApplicationRoles.Responder, Permissions.ResourcesAssign, false)]
    [InlineData(ApplicationRoles.Operator, Permissions.ResourcesAssign, true)]
    [InlineData(ApplicationRoles.Operator, Permissions.ResourcesDispatch, false)]
    [InlineData(ApplicationRoles.EmergencyAdmin, Permissions.CopilotActionsApprove, true)]
    [InlineData(ApplicationRoles.EmergencyAdmin, Permissions.AuditRead, false)]
    [InlineData(ApplicationRoles.SystemAdmin, Permissions.AuditRead, true)]
    [InlineData(ApplicationRoles.SystemAdmin, Permissions.DisasterRecoveryExecute, true)]
    public void Permission_IsGrantedExactlyAsMapped(string role, string permission, bool expected)
    {
        Assert.Equal(expected, RolePermissions.HasPermission([role], permission));
    }

    [Fact]
    public void EveryRoleHasAnExplicitPermissionSet()
    {
        Assert.Equal(
            new[] { ApplicationRoles.Citizen, ApplicationRoles.Responder, ApplicationRoles.Operator,
                ApplicationRoles.EmergencyAdmin, ApplicationRoles.SystemAdmin }
                .Order(StringComparer.OrdinalIgnoreCase),
            RolePermissions.Map.Keys.Order(StringComparer.OrdinalIgnoreCase));
        Assert.All(RolePermissions.Map.Values, permissions => Assert.NotNull(permissions));
    }

    [Fact]
    public void EveryRoleAndDeclaredPermission_MatchesThePermissionMap()
    {
        var permissions = typeof(Permissions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToArray();

        foreach (var (role, grants) in RolePermissions.Map)
        {
            foreach (var permission in permissions)
            {
                Assert.Equal(
                    grants.Contains(permission, StringComparer.OrdinalIgnoreCase),
                    RolePermissions.HasPermission([role], permission));
            }
        }
    }
}
