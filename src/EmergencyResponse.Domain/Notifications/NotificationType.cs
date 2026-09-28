namespace EmergencyResponse.Domain.Notifications;

public enum NotificationType
{
    IncidentCreated = 1,
    IncidentSeverityChanged = 2,
    IncidentPriorityChanged = 3,

    AIReviewRequired = 10,
    DuplicateReviewRequired = 11,
    VisualReviewRequired = 12,

    EscalationOpened = 20,
    EscalationAcknowledged = 21,
    EscalationResolved = 22,

    SituationCreated = 30,
    SituationExpanded = 31,
    SituationLevelChanged = 32,
    SituationReviewRequired = 33,

    ResourceShortage = 40,
    ResourceAssignment = 41,
    ResourceStatusChanged = 42,

    System = 90
}
