namespace EmergencyResponse.Application.AISecurity;

public interface IAIInputSecurityService
{
    Task<AIInputSecurityResult> EvaluateAsync(
        string userPrompt,
        IReadOnlyCollection<AIUntrustedContent> untrustedContent,
        CancellationToken cancellationToken = default);
}
