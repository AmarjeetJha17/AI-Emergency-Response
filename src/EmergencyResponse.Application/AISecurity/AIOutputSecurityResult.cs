namespace EmergencyResponse.Application.AISecurity;

public sealed class AIOutputSecurityResult
{
    public bool Allowed { get; init; }
    public bool SchemaValid { get; init; }
    public bool ContainsUnsafeInstruction { get; init; }
    public bool ContainsSensitiveData { get; init; }
    public bool GroundingValid { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyList<string> Signals { get; init; } = Array.Empty<string>();
}
