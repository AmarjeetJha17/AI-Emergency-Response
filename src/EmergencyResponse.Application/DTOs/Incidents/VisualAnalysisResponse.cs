using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.DTOs.Incidents;

public sealed record VisualAnalysisResponse
{
    public Guid Id { get; init; }

    public Guid IncidentMediaId { get; init; }

    public VisualAnalysisStatus Status { get; init; }

    public string SceneDescription { get; init; } = string.Empty;

    public IReadOnlyList<string> DetectedHazards { get; init; } = [];

    public IReadOnlyList<string> VisibleEmergencyIndicators { get; init; } = [];

    public int? EstimatedPeopleAffected { get; init; }

    public int? EstimatedVehiclesAffected { get; init; }

    public bool PeopleAppearTrapped { get; init; }

    public bool RoadAppearsBlocked { get; init; }

    public bool StructuralDamageVisible { get; init; }

    public bool FireVisible { get; init; }

    public bool FloodingVisible { get; init; }

    public bool SmokeVisible { get; init; }

    public bool ImmediateDangerVisible { get; init; }

    public string? RecommendedResponse { get; init; }

    public double Confidence { get; init; }

    public bool HumanReviewed { get; init; }

    public string? HumanDecision { get; init; }

    public int? FramesAnalyzed { get; init; }

    public double? VideoDurationSeconds { get; init; }

    public IReadOnlyList<VisualFrameFindingResponse> FrameFindings { get; init; } = [];

    // Null = not yet reviewed; "Accepted" or "Rejected" after operator review
    public string? HumanReviewOutcome =>
        HumanReviewed
            ? (Status == VisualAnalysisStatus.Rejected ? "Rejected" : "Accepted")
            : null;
}
