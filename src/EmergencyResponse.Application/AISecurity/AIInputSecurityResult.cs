namespace EmergencyResponse.Application.AISecurity;

public sealed class AIInputSecurityResult
{
    public bool Allowed { get; init; }
    public bool PromptAttackDetected { get; init; }
    public bool DocumentAttackDetected { get; init; }
    public bool SuspiciousContentDetected { get; init; }
    public string? Reason { get; init; }
    public string? DetectionSource { get; init; }
    public DateTimeOffset EvaluatedAtUtc { get; init; }
    public IReadOnlyList<string> Signals { get; init; } = Array.Empty<string>();
}
