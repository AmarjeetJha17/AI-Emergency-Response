using EmergencyResponse.Application.AISecurity;
using EmergencyResponse.Application.Observability;

namespace EmergencyResponse.Infrastructure.AI;

public sealed class AIInputSecurityService : IAIInputSecurityService
{
    private readonly IPromptShieldClient _promptShield;
    private readonly IAISecurityPolicy _policy;

    public AIInputSecurityService(
        IPromptShieldClient promptShield,
        IAISecurityPolicy policy)
    {
        _promptShield = promptShield;
        _policy = policy;
    }

    public async Task<AIInputSecurityResult> EvaluateAsync(
        string userPrompt,
        IReadOnlyCollection<AIUntrustedContent> untrustedContent,
        CancellationToken cancellationToken = default)
    {
        var documents = untrustedContent
            .Where(x => x.Boundary is not TrustedContentBoundary.UserInput)
            .Select(x => x.Content)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(5)
            .ToArray();
        var result = await _promptShield.AnalyzeAsync(
            userPrompt,
            documents,
            cancellationToken);
        var blocked = (_policy.BlockOnPromptAttack && result.PromptAttackDetected) ||
            (_policy.BlockOnDocumentAttack && result.DocumentAttackDetected);
        if (result.PromptAttackDetected)
        {
            EmergencyResponseTelemetry.AIPromptAttacksDetected.Add(1);
        }
        if (result.DocumentAttackDetected)
        {
            EmergencyResponseTelemetry.AIDocumentAttacksDetected.Add(1);
        }

        return new AIInputSecurityResult
        {
            Allowed = !blocked,
            PromptAttackDetected = result.PromptAttackDetected,
            DocumentAttackDetected = result.DocumentAttackDetected,
            SuspiciousContentDetected = result.AttackDetected,
            Reason = blocked
                ? "Untrusted content contained a prompt-injection signal."
                : null,
            DetectionSource = result.IsAvailable
                ? "deterministic-prompt-shield"
                : "prompt-shield-unavailable",
            EvaluatedAtUtc = DateTimeOffset.UtcNow,
            Signals = result.Signals
        };
    }
}
