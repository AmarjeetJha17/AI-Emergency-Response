namespace EmergencyResponse.Domain.Enums;

public enum IncidentStatus
{
    Submitted = 1,
    UnderReview = 2,
    Assigned = 3,
    InProgress = 4,
    Escalated = 5,
    Resolved = 6,
    Closed = 7,
    Rejected = 8
}