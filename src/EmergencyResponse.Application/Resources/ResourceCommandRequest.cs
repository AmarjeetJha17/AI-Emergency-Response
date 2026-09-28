namespace EmergencyResponse.Application.Resources;

public sealed record ResourceCommandRequest
{
    public ResourceCommand Command { get; init; }

    public string? Notes { get; init; }
}
