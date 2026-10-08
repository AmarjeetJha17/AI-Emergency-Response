using EmergencyResponse.Application.Common.Roles;

namespace EmergencyResponse.Application.Security.Authorization;

public static class RolePermissions
{
    private static readonly string[] SystemAdminPermissions =
    [
        Permissions.IncidentsRead,
        Permissions.IncidentsCreate,
        Permissions.IncidentsUpdate,
        Permissions.ResourcesRead,
        Permissions.ResourcesAssign,
        Permissions.ResourcesDispatch,
        Permissions.EscalationsRead,
        Permissions.EscalationsAcknowledge,
        Permissions.EscalationsResolve,
        Permissions.SituationsRead,
        Permissions.SituationsAcknowledge,
        Permissions.SituationsResolve,
        Permissions.KnowledgeRead,
        Permissions.KnowledgeManage,
        Permissions.CopilotUse,
        Permissions.CopilotActionsPropose,
        Permissions.CopilotActionsApprove,
        Permissions.AuditRead,
        Permissions.DisasterRecoveryExecute
    ];

    public static readonly IReadOnlyDictionary<string, string[]> Map =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            [ApplicationRoles.Citizen] =
            [
                Permissions.IncidentsCreate
            ],
            [ApplicationRoles.Responder] =
            [
                Permissions.IncidentsRead,
                Permissions.ResourcesRead
            ],
            [ApplicationRoles.Operator] =
            [
                Permissions.IncidentsRead,
                Permissions.IncidentsUpdate,
                Permissions.ResourcesRead,
                Permissions.ResourcesAssign,
                Permissions.EscalationsRead,
                Permissions.EscalationsAcknowledge,
                Permissions.SituationsRead,
                Permissions.SituationsAcknowledge,
                Permissions.KnowledgeRead,
                Permissions.CopilotUse,
                Permissions.CopilotActionsPropose
            ],
            [ApplicationRoles.EmergencyAdmin] =
            [
                ..SystemAdminPermissions
                    .Where(x => x != Permissions.AuditRead && x != Permissions.DisasterRecoveryExecute)
            ],
            [ApplicationRoles.SystemAdmin] = SystemAdminPermissions
        };

    public static bool HasPermission(
        IEnumerable<string> roles,
        string permission)
    {
        return roles.Any(role =>
            Map.TryGetValue(role, out var permissions) &&
            permissions.Contains(
                permission,
                StringComparer.OrdinalIgnoreCase));
    }
}
