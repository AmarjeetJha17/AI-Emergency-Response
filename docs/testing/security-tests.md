# Security Test Plan

Authorization expectations follow `Application.Security.Authorization.RolePermissions`. Audit access is SystemAdmin-only in the current map; DisasterRecoveryExecute is also SystemAdmin-only. Tests must fail when an endpoint is accessible without its mapped permission.

| Operation | Anonymous | Citizen | Responder | Operator | EmergencyAdmin | SystemAdmin |
|---|---:|---:|---:|---:|---:|---:|
| Create incident | Deny | Allow | Deny | Deny | Allow | Allow |
| View own incident | Deny | Scoped | Deny | Allow | Allow | Allow |
| Operational incident read | Deny | Deny | Allow (scope) | Allow | Allow | Allow |
| Assign resource | Deny | Deny | Deny | Allow | Allow | Allow |
| Dispatch resource | Deny | Deny | Deny | Deny | Allow | Allow |
| Resolve escalation | Deny | Deny | Deny | Deny | Allow | Allow |
| Resolve situation | Deny | Deny | Deny | Deny | Allow | Allow |
| Approve Copilot action | Deny | Deny | Deny | Deny | Allow | Allow |
| Manage knowledge | Deny | Deny | Deny | Deny | Allow | Allow |
| Read audit | Deny | Deny | Deny | Deny | Deny | Allow |

Test IDOR for incidents, incident media, AI/visual analysis, situations, escalations, resource assignments, notifications, knowledge documents, Copilot conversations, and action proposals. Include cross-department operator denial. Test rate-limit threshold and `429`/`Retry-After`, including equivalent route paths. Test exports with the same authorization and filtering as interactive reads. Add secret-safe logging assertions for tokens, cookies, prompts, files, and connection strings.
