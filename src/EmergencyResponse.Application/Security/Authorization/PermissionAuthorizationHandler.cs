using System.Security.Claims;

using Microsoft.AspNetCore.Authorization;

namespace EmergencyResponse.Application.Security.Authorization;

public sealed class PermissionAuthorizationHandler
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var roles = context.User
            .FindAll(ClaimTypes.Role)
            .Select(x => x.Value);

        if (RolePermissions.HasPermission(
                roles,
                requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
