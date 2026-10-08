namespace EmergencyResponse.Domain.Knowledge;

public enum SecurityScanStatus
{
    Pending = 1,
    Processing = 2,
    Passed = 3,
    Blocked = 4,
    Failed = 5,
    RequiresReview = 6
}
