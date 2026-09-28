namespace EmergencyResponse.Application.Realtime;

public static class SituationUpdateReasons
{
    public const string Created = "SituationCreated";
    public const string Expanded = "SituationExpanded";
    public const string Updated = "SituationUpdated";
    public const string IncidentAdded = "SituationIncidentAdded";
    public const string LevelEscalated = "SituationLevelEscalated";
    public const string Recalculated = "SituationRecalculated";
    public const string AIAnalyzed = "SituationAIAnalyzed";
    public const string Reviewed = "SituationReviewed";
    public const string Acknowledged = "SituationAcknowledged";
    public const string Resolved = "SituationResolved";
    public const string Dismissed = "SituationDismissed";
}
