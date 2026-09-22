namespace EmergencyResponse.Domain.Enums;

public enum ResourceStatus
{
    Available = 1,
    Assigned = 2,
    Dispatched = 3,
    EnRoute = 4,
    OnScene = 5,
    Returning = 6,
    Unavailable = 7,
    Maintenance = 8
}
