namespace EmergencyResponse.Application.Security.Authorization;

public static class Permissions
{
    public const string IncidentsRead = "incidents.read";
    public const string IncidentsCreate = "incidents.create";
    public const string IncidentsUpdate = "incidents.update";

    public const string ResourcesRead = "resources.read";
    public const string ResourcesAssign = "resources.assign";
    public const string ResourcesDispatch = "resources.dispatch";

    public const string EscalationsRead = "escalations.read";
    public const string EscalationsAcknowledge = "escalations.acknowledge";
    public const string EscalationsResolve = "escalations.resolve";

    public const string SituationsRead = "situations.read";
    public const string SituationsAcknowledge = "situations.acknowledge";
    public const string SituationsResolve = "situations.resolve";

    public const string KnowledgeRead = "knowledge.read";
    public const string KnowledgeManage = "knowledge.manage";

    public const string CopilotUse = "copilot.use";
    public const string CopilotActionsPropose = "copilot.actions.propose";
    public const string CopilotActionsApprove = "copilot.actions.approve";

    public const string AuditRead = "audit.read";
    public const string DisasterRecoveryExecute = "disaster-recovery.execute";
}
