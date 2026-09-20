namespace EmergencyResponse.Web.Models.Dashboard;

/// <summary>
/// View model fed directly into the Syncfusion DataGrid.
/// Enum fields are stored as strings so the JS cell templates
/// can compare values like Severity === 'Critical' without
/// needing to know the underlying integer representation.
/// </summary>
public sealed class DashboardIncidentGridItem
{
    public Guid Id { get; init; }

    public string IncidentNumber { get; init; } = null!;

    public string Title { get; init; } = null!;

    public string? IncidentType { get; init; }

    /// <summary>Low | Moderate | High | Critical</summary>
    public string Severity { get; init; } = null!;

    /// <summary>Low | Normal | High | Critical</summary>
    public string Priority { get; init; } = null!;

    /// <summary>Submitted | UnderReview | Assigned | InProgress | Escalated | Resolved | Closed | Rejected</summary>
    public string Status { get; init; } = null!;

    public DateTime CreatedAtUtc { get; init; }

    public bool IsAssigned { get; init; }

    public bool RequiresAIReview { get; init; }

    public bool HasDuplicateCandidates { get; init; }

    public bool HasVisualReview { get; init; }

    public string DetailsUrl { get; init; } = null!;
}
