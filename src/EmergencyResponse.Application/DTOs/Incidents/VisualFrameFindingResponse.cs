namespace EmergencyResponse.Application.DTOs.Incidents;

public sealed record VisualFrameFindingResponse
{
    public int FrameNumber { get; init; }

    public double TimestampSeconds { get; init; }

    public string SceneDescription { get; init; } = string.Empty;

    public IReadOnlyList<string> DetectedHazards { get; init; } = [];

    public int? EstimatedPeopleAffected { get; init; }

    public int? EstimatedVehiclesAffected { get; init; }

    public bool PeopleAppearTrapped { get; init; }

    public bool RoadAppearsBlocked { get; init; }

    public bool StructuralDamageVisible { get; init; }

    public bool FireVisible { get; init; }

    public bool FloodingVisible { get; init; }

    public bool SmokeVisible { get; init; }

    public bool ImmediateDangerVisible { get; init; }

    public double Confidence { get; init; }
}
