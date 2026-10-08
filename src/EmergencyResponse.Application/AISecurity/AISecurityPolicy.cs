namespace EmergencyResponse.Application.AISecurity;

public sealed class AISecurityPolicy : IAISecurityPolicy
{
    public bool BlockOnPromptAttack { get; init; } = true;
    public bool BlockOnDocumentAttack { get; init; } = true;
    public bool BlockOnSensitiveOutput { get; init; } = true;
    public bool BlockOnUnsafeOutput { get; init; } = true;
}
