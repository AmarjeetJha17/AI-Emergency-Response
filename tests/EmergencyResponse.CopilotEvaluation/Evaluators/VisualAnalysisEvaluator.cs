using EmergencyResponse.Application.AI.VisualAnalysis;

namespace EmergencyResponse.CopilotEvaluation.Evaluators;

public sealed record VisualExpectedFacts(
    bool FloodingVisible,
    bool FireVisible,
    bool SmokeVisible,
    bool StructuralDamageVisible,
    bool RoadAppearsBlocked,
    bool PeopleAppearTrapped,
    bool ImmediateDangerVisible,
    bool HasEmergency);

public sealed class VisualAnalysisEvaluator
{
    public VisualAnalysisCaseScore Evaluate(VisualExpectedFacts expected, VisualAnalysisResult actual)
    {
        var rows = new[]
        {
            (expected.FloodingVisible, actual.FloodingVisible),
            (expected.FireVisible, actual.FireVisible),
            (expected.SmokeVisible, actual.SmokeVisible),
            (expected.StructuralDamageVisible, actual.StructuralDamageVisible),
            (expected.RoadAppearsBlocked, actual.RoadAppearsBlocked),
            (expected.PeopleAppearTrapped, actual.PeopleAppearTrapped),
            (expected.ImmediateDangerVisible, actual.ImmediateDangerVisible)
        };
        var binary = EmergencyResponse.CopilotEvaluation.Infrastructure.BinaryMetric.From(rows);
        var critical = new[]
        {
            (expected.PeopleAppearTrapped, actual.PeopleAppearTrapped),
            (expected.ImmediateDangerVisible, actual.ImmediateDangerVisible),
            (expected.StructuralDamageVisible, actual.StructuralDamageVisible),
            (expected.FireVisible, actual.FireVisible),
            (expected.FloodingVisible, actual.FloodingVisible),
            (expected.RoadAppearsBlocked, actual.RoadAppearsBlocked)
        };
        var criticalBinary = EmergencyResponse.CopilotEvaluation.Infrastructure.BinaryMetric.From(critical);
        var actualEmergency = actual.DetectedHazards.Count > 0 || actual.VisibleEmergencyIndicators.Count > 0 ||
                              actual.ImmediateDangerVisible || actual.FloodingVisible || actual.FireVisible ||
                              actual.SmokeVisible || actual.StructuralDamageVisible || actual.RoadAppearsBlocked ||
                              actual.PeopleAppearTrapped;
        return new VisualAnalysisCaseScore(
            binary.Precision,
            binary.Recall,
            criticalBinary.Recall,
            expected.HasEmergency == actualEmergency,
            double.IsFinite(actual.Confidence) && actual.Confidence is >= 0 and <= 1);
    }
}

public sealed class VideoHazardPreservationEvaluator
{
    public bool Evaluate(VisualAnalysisResult result)
        => result.FrameFindings.Count > 0 &&
           (!result.FrameFindings.Any(x => x.PeopleAppearTrapped) || result.PeopleAppearTrapped) &&
           (!result.FrameFindings.Any(x => x.ImmediateDangerVisible) || result.ImmediateDangerVisible) &&
           (!result.FrameFindings.Any(x => x.StructuralDamageVisible) || result.StructuralDamageVisible) &&
           (!result.FrameFindings.Any(x => x.FireVisible) || result.FireVisible) &&
           (!result.FrameFindings.Any(x => x.FloodingVisible) || result.FloodingVisible) &&
           (!result.FrameFindings.Any(x => x.RoadAppearsBlocked) || result.RoadAppearsBlocked);
}

public sealed record VisualAnalysisCaseScore(
    double HazardPrecision,
    double HazardRecall,
    double CriticalHazardRecall,
    bool EmergencyClassificationCorrect,
    bool ConfidenceInRange);
