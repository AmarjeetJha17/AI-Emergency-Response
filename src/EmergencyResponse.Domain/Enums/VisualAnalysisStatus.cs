namespace EmergencyResponse.Domain.Enums;

public enum VisualAnalysisStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    RequiresReview = 4,
    Rejected = 5
}