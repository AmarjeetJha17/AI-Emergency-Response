namespace EmergencyResponse.Application.AISecurity;

public sealed class TrustedToolResultEnvelope
{
    public string ToolName { get; init; } = string.Empty;
    public object? Data { get; init; }
    public DateTimeOffset RetrievedAtUtc { get; init; }
    public bool IsAuthoritativeOperationalState { get; init; }
}
