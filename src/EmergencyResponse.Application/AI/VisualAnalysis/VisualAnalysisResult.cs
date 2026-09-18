namespace EmergencyResponse.Application.AI.VisualAnalysis;

public sealed record VisualAnalysisResult
{
    public string SceneDescription { get; init; } = string.Empty;

    public List<string> DetectedHazards { get; init; } = [];

    public List<string> VisibleEmergencyIndicators { get; init; } = [];

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

    public List<VisualFrameFinding> FrameFindings { get; init; } = [];
}