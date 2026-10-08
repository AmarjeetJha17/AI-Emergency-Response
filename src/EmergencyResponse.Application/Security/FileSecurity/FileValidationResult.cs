namespace EmergencyResponse.Application.Security.FileSecurity;

public sealed class FileValidationResult
{
    private FileValidationResult(
        bool isValid,
        string? safeExtension,
        string? failureReason)
    {
        IsValid = isValid;
        SafeExtension = safeExtension;
        FailureReason = failureReason;
    }

    public bool IsValid { get; }

    public string? SafeExtension { get; }

    public string? FailureReason { get; }

    public static FileValidationResult Valid(string safeExtension) =>
        new(true, safeExtension, null);

    public static FileValidationResult Invalid(string reason) =>
        new(false, null, reason);
}
