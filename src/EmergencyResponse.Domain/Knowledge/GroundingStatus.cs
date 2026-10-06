namespace EmergencyResponse.Domain.Knowledge;

public enum GroundingStatus
{
    Grounded = 1,
    PartiallyGrounded = 2,
    InsufficientEvidence = 3,
    SearchUnavailable = 4,
    ValidationFailed = 5
}
