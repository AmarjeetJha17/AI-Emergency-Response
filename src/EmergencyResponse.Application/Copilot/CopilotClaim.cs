namespace EmergencyResponse.Application.Copilot;

public sealed class CopilotClaim
{
    public string Claim { get; init; } = string.Empty;

    public IReadOnlyList<string> SupportingSourceIds { get; init; } = [];

    public bool IsSupported { get; init; }

    public string? Reason { get; init; }
}
