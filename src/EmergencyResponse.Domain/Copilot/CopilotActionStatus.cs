namespace EmergencyResponse.Domain.Copilot;

public enum CopilotActionStatus
{
    Proposed = 1,
    AwaitingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Expired = 5,
    Executing = 6,
    Succeeded = 7,
    Failed = 8,
    Cancelled = 9
}
