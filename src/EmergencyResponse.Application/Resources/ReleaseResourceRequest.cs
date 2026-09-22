namespace EmergencyResponse.Application.Resources;

public sealed record ReleaseResourceRequest
{
    public string? Notes { get; init; }
}
