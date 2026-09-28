namespace EmergencyResponse.Application.Situations.DTOs;

public sealed record ReviewSituationRequest
{
    public string Decision { get; init; } = null!;

    public string? Notes { get; init; }
}
