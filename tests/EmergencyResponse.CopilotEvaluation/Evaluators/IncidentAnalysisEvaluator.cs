using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.CopilotEvaluation.Infrastructure;

namespace EmergencyResponse.CopilotEvaluation.Evaluators;

public sealed record IncidentExpectedFacts(
    string IncidentType,
    string Department,
    bool PeopleTrapped,
    bool RoadBlocked,
    bool ImmediateDanger,
    int? PeopleAffected,
    int? VehiclesAffected,
    int? Casualties,
    double MinimumConfidence = 0,
    double MaximumConfidence = 1);

public sealed class IncidentAnalysisEvaluator
{
    public IncidentAnalysisCaseScore Evaluate(IncidentExpectedFacts expected, IncidentAnalysisResult actual)
    {
        var facts = new[]
        {
            (expected.PeopleTrapped, actual.Facts.PeopleTrapped),
            (expected.RoadBlocked, actual.Facts.RoadBlocked),
            (expected.ImmediateDanger, actual.Facts.ImmediateDanger)
        };
        var binary = BinaryMetric.From(facts);
        return new IncidentAnalysisCaseScore
        {
            IncidentTypeCorrect = Equal(expected.IncidentType, actual.IncidentType),
            DepartmentCorrect = Equal(expected.Department, actual.RecommendedDepartment),
            CriticalFactRecall = binary.Recall,
            CriticalFactPrecision = binary.Precision,
            PeopleAffectedAbsoluteError = AbsoluteError(expected.PeopleAffected, actual.PeopleAffected),
            VehiclesAffectedAbsoluteError = AbsoluteError(expected.VehiclesAffected, actual.VehiclesAffected),
            CasualtiesAbsoluteError = AbsoluteError(expected.Casualties, actual.Casualties),
            ConfidenceInRange = double.IsFinite(actual.Confidence) &&
                                actual.Confidence >= expected.MinimumConfidence &&
                                actual.Confidence <= expected.MaximumConfidence
        };
    }

    private static bool Equal(string expected, string actual)
        => string.Equals(expected.Trim(), actual.Trim(), StringComparison.OrdinalIgnoreCase);

    private static double? AbsoluteError(int? expected, int? actual)
        => expected is null || actual is null ? null : Math.Abs(expected.Value - actual.Value);
}

public sealed class IncidentAnalysisCaseScore
{
    public bool IncidentTypeCorrect { get; init; }
    public bool DepartmentCorrect { get; init; }
    public double CriticalFactRecall { get; init; }
    public double CriticalFactPrecision { get; init; }
    public double? PeopleAffectedAbsoluteError { get; init; }
    public double? VehiclesAffectedAbsoluteError { get; init; }
    public double? CasualtiesAbsoluteError { get; init; }
    public bool ConfidenceInRange { get; init; }
}
