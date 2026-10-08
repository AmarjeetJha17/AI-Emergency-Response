namespace EmergencyResponse.Application.AISecurity;

public static class AISecurityEventTypes
{
    public const string PromptAttackDetected = "AI_PROMPT_ATTACK_DETECTED";
    public const string DocumentAttackDetected = "AI_DOCUMENT_ATTACK_DETECTED";
    public const string ToolOutputAttackDetected = "AI_TOOL_OUTPUT_ATTACK_DETECTED";
    public const string OutputSecurityBlocked = "AI_OUTPUT_SECURITY_BLOCKED";
    public const string SensitiveDataDetected = "AI_SENSITIVE_DATA_DETECTED";
    public const string GroundingValidationFailed = "AI_GROUNDING_VALIDATION_FAILED";
    public const string ActionApprovalBypassBlocked = "AI_ACTION_APPROVAL_BYPASS_BLOCKED";
}
