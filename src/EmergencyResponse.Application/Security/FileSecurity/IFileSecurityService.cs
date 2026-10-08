namespace EmergencyResponse.Application.Security.FileSecurity;

public interface IFileSecurityService
{
    Task<FileValidationResult> ValidateAsync(
        Stream content,
        string fileName,
        string declaredContentType,
        long declaredLength,
        CancellationToken cancellationToken = default);
}
