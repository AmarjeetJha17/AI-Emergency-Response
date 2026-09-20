namespace EmergencyResponse.Application.DTOs.Incidents;

public sealed record IncidentStatusHistoryResponse
{
    public string OldStatus { get; init; } = null!;

    public string NewStatus { get; init; } = null!;

    public string? ChangedByUserId { get; init; }

    public string? Remarks { get; init; }

    public DateTime ChangedAtUtc { get; init; }
}
