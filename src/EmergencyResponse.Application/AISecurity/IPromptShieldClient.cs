namespace EmergencyResponse.Application.AISecurity;

public interface IPromptShieldClient
{
    Task<PromptShieldResult> AnalyzeAsync(
        string userPrompt,
        IReadOnlyCollection<string> documents,
        CancellationToken cancellationToken = default);
}

public sealed class PromptShieldResult
{
    public bool AttackDetected { get; init; }
    public bool PromptAttackDetected { get; init; }
    public bool DocumentAttackDetected { get; init; }
    public bool IsAvailable { get; init; }
    public IReadOnlyList<string> Signals { get; init; } = Array.Empty<string>();
}
