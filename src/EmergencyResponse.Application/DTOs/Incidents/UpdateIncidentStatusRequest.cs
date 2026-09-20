namespace EmergencyResponse.Application.DTOs.Incidents;

public sealed record UpdateIncidentStatusRequest
{
    /// <summary>Target status (e.g. "UnderReview", "InProgress", "Resolved").</summary>
    public string Status { get; init; } = null!;

    /// <summary>Optional notes recorded in status history.</summary>
    public string? Remarks { get; init; }
}
