namespace EmergencyResponse.Application.Situations.DTOs;

public sealed record SituationActionRequest
{
    public string? Notes { get; init; }
}
