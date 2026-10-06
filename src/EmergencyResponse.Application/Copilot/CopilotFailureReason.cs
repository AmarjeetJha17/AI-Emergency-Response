namespace EmergencyResponse.Application.Copilot;

public enum CopilotFailureReason
{
    KnowledgeUnavailable = 1,
    ToolUnavailable = 2,
    AuthorizationDenied = 3,
    CitationValidationFailed = 4,
    PolicyValidationFailed = 5,
    ModelFailure = 6,
    SafetyBlocked = 7,
    InsufficientEvidence = 8
}
