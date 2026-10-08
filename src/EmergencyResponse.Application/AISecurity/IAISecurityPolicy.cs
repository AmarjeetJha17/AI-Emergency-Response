namespace EmergencyResponse.Application.AISecurity;

public interface IAISecurityPolicy
{
    bool BlockOnPromptAttack { get; }
    bool BlockOnDocumentAttack { get; }
    bool BlockOnSensitiveOutput { get; }
    bool BlockOnUnsafeOutput { get; }
}
