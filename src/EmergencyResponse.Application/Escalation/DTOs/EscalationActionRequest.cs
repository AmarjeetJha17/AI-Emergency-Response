namespace EmergencyResponse.Application.Escalation.DTOs;

public sealed record EscalationActionRequest
{
    public string? Notes { get; init; }
}
