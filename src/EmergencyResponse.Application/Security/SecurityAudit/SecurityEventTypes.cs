namespace EmergencyResponse.Application.Security.SecurityAudit;

public static class SecurityEventTypes
{
    public const string LoginSucceeded = "SEC_LOGIN_SUCCEEDED";
    public const string LoginFailed = "SEC_LOGIN_FAILED";
    public const string Logout = "SEC_LOGOUT";
    public const string AuthorizationDenied = "SEC_AUTHORIZATION_DENIED";
    public const string RateLimitExceeded = "SEC_RATE_LIMIT_EXCEEDED";
    public const string InvalidFileUpload = "SEC_INVALID_FILE_UPLOAD";
    public const string TokenRejected = "SEC_TOKEN_REJECTED";
    public const string CsrfRejected = "SEC_CSRF_REJECTED";
    public const string SuspiciousCopilotRequest = "SEC_SUSPICIOUS_COPILOT_REQUEST";
    public const string ActionApprovalDenied = "SEC_ACTION_APPROVAL_DENIED";
    public const string SensitiveDataAccess = "SEC_SENSITIVE_DATA_ACCESS";
    public const string SensitiveDataExport = "SEC_SENSITIVE_DATA_EXPORT";
    public const string DataRetentionExecuted = "SEC_DATA_RETENTION_EXECUTED";
    public const string MediaAccess = "SEC_MEDIA_ACCESS";
    public const string KnowledgeDocumentAccess = "SEC_KNOWLEDGE_DOCUMENT_ACCESS";
}
