namespace EmergencyResponse.Application.AI.VisualAnalysis;

public static class VisualAnalysisAggregator
{
    public static VisualAnalysisResult Aggregate(
        IReadOnlyList<VisualFrameFinding> findings,
        string? recommendedResponse,
        IReadOnlyList<string> visibleEmergencyIndicators)
    {
        if (findings.Count == 0)
        {
            throw new InvalidOperationException(
                "Cannot aggregate zero frame findings.");
        }

        return new VisualAnalysisResult
        {
            SceneDescription = findings[findings.Count / 2].SceneDescription,

            DetectedHazards = findings
                .SelectMany(f => f.DetectedHazards)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),

            VisibleEmergencyIndicators =
                visibleEmergencyIndicators.ToList(),

            EstimatedPeopleAffected = findings
                .Where(f => f.EstimatedPeopleAffected.HasValue)
                .Select(f => f.EstimatedPeopleAffected)
                .DefaultIfEmpty(null)
                .Max(),

            EstimatedVehiclesAffected = findings
                .Where(f => f.EstimatedVehiclesAffected.HasValue)
                .Select(f => f.EstimatedVehiclesAffected)
                .DefaultIfEmpty(null)
                .Max(),

            PeopleAppearTrapped  = findings.Any(f => f.PeopleAppearTrapped),
            RoadAppearsBlocked   = findings.Any(f => f.RoadAppearsBlocked),
            StructuralDamageVisible = findings.Any(f => f.StructuralDamageVisible),
            FireVisible          = findings.Any(f => f.FireVisible),
            FloodingVisible      = findings.Any(f => f.FloodingVisible),
            SmokeVisible         = findings.Any(f => f.SmokeVisible),
            ImmediateDangerVisible = findings.Any(f => f.ImmediateDangerVisible),

            RecommendedResponse = recommendedResponse,

            Confidence = findings.Average(f => f.Confidence),

            FrameFindings = findings.ToList()
        };
    }
}
