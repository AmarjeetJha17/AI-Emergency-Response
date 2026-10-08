using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Application.Security.Authorization;

public sealed class PermissionPolicyProvider
    : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(
        IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(
        string policyName)
    {
        if (!RolePermissions.Map.Values
                .SelectMany(x => x)
                .Contains(
                    policyName,
                    StringComparer.OrdinalIgnoreCase))
        {
            return base.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder()
            .AddRequirements(
                new PermissionRequirement(policyName))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
