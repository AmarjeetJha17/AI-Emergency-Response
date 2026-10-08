using EmergencyResponse.Application.Security.Authorization;

namespace EmergencyResponse.UnitTests.Security;

public sealed class PermissionTests
{
    [Fact]
    public void Citizen_can_create_incidents_but_cannot_approve_actions()
    {
        Assert.True(
            RolePermissions.HasPermission(
                ["Citizen"],
                Permissions.IncidentsCreate));
        Assert.False(
            RolePermissions.HasPermission(
                ["Citizen"],
                Permissions.CopilotActionsApprove));
        Assert.Equal(
            [Permissions.IncidentsCreate],
            RolePermissions.Map["Citizen"]);
    }

    [Fact]
    public void Operator_can_propose_but_not_approve_copilot_actions()
    {
        Assert.True(
            RolePermissions.HasPermission(
                ["Operator"],
                Permissions.CopilotActionsPropose));
        Assert.False(
            RolePermissions.HasPermission(
                ["Operator"],
                Permissions.CopilotActionsApprove));
    }

    [Fact]
    public void System_admin_has_all_defined_permissions()
    {
        foreach (var permission in typeof(Permissions)
                     .GetFields(
                         System.Reflection.BindingFlags.Public |
                         System.Reflection.BindingFlags.Static)
                     .Where(x => x.FieldType == typeof(string))
                     .Select(x => (string)x.GetValue(null)!))
        {
            Assert.True(
                RolePermissions.HasPermission(
                    ["SystemAdmin"],
                    permission));
        }
    }
}
