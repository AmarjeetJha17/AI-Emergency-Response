using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.CopilotEvaluation.Evaluators;
using EmergencyResponse.CopilotEvaluation.Infrastructure;

namespace EmergencyResponse.UnitTests.AI;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.AI)]
public sealed class EvaluationEvaluatorTests
{
    [Fact]
    public void BinaryMetric_ReportsPrecisionRecallAndF1()
    {
        var metric = BinaryMetric.From(
        [
            (true, true),
            (true, false),
            (false, true),
            (false, false)
        ]);

        Assert.Equal(0.5, metric.Precision);
        Assert.Equal(0.5, metric.Recall);
        Assert.Equal(0.5, metric.F1);
    }

    [Fact]
    public void CitationEvaluator_RequiresExistingRetrievedSupportingSource()
    {
        var evaluator = new CitationEvaluator();
        var retrieved = new HashSet<string>(["DOC-A"], StringComparer.OrdinalIgnoreCase);
        var existing = new HashSet<string>(["DOC-A", "DOC-B"], StringComparer.OrdinalIgnoreCase);

        Assert.True(evaluator.IsCorrect("DOC-A", retrieved, existing, claimSupportedBySource: true));
        Assert.False(evaluator.IsCorrect("DOC-B", retrieved, existing, claimSupportedBySource: true));
        Assert.False(evaluator.IsCorrect("DOC-A", retrieved, existing, claimSupportedBySource: false));
    }

    [Fact]
    public void GroundingEvaluator_PenalizesUnsupportedClaimsAndMissingFacts()
    {
        var score = new GroundingEvaluator().Evaluate(
            new HashSet<string>(["perimeter", "threats"]),
            new HashSet<string>(["perimeter", "threats"]),
            new HashSet<string>(["perimeter", "unsupported detail"]));

        Assert.Equal(0.5, score.Groundedness);
        Assert.Equal(0.5, score.UnsupportedClaimRate);
        Assert.Equal(0.5, score.RequiredFactCoverage);
    }

    [Fact]
    public void ToolSelectionEvaluator_RejectsMissingAndForbiddenTools()
    {
        var evaluator = new ToolSelectionEvaluator();
        var score = evaluator.Evaluate(
            new HashSet<string>(["incident", "environment"]),
            new HashSet<string>(["incident", "dispatch"]),
            new HashSet<string>(["dispatch"]),
            new Dictionary<string, string> { ["incidentId"] = "INC-1" },
            new Dictionary<string, string> { ["incidentId"] = "INC-1" });

        Assert.False(score.Passed);
        Assert.True(score.ForbiddenToolSelected);
        Assert.Equal(0.5, score.Recall);
    }

    [Fact]
    public void ActionSafetyEvaluator_RequiresExactlyOneApprovedValidSideEffect()
    {
        var evaluator = new ActionSafetyEvaluator();
        Assert.True(evaluator.Evaluate(true, 1, true, true, false, true, true).Passed);
        Assert.False(evaluator.Evaluate(true, 1, true, false, false, true, true).Passed);
        Assert.False(evaluator.Evaluate(true, 2, true, true, false, true, true).Passed);
    }

    [Fact]
    public void PredictionEvaluator_ComputesForecastAndClassificationMetrics()
    {
        var evaluator = new PredictionEvaluator();
        var forecast = evaluator.EvaluateForecast([10, 20, 30], [9, 22, 30]);
        var classification = evaluator.EvaluateClassification(
            [true, true, false, false], [true, false, false, false]);

        Assert.Equal(1, forecast.MeanAbsoluteError);
        Assert.Equal(Math.Sqrt(5.0 / 3.0), forecast.RootMeanSquaredError, 8);
        Assert.Equal(1, classification.Precision);
        Assert.Equal(0.5, classification.Recall);
        Assert.Equal(0.75, classification.Accuracy);

        var probability = evaluator.EvaluateClassification(
            [true, true, false, false], [0.9, 0.6, 0.4, 0.1]);
        Assert.Equal(1d, probability.RocAuc!.Value);
        Assert.Equal(1d, probability.PrAuc!.Value);
        Assert.Equal(0.085, probability.BrierScore!.Value, 8);
    }

    [Fact]
    public void IncidentAnalysisEvaluator_ChecksFactsAndConfidenceRange()
    {
        var expected = new IncidentExpectedFacts(
            "Flood", "Rescue", true, true, true, 4, 2, 0, 0.7, 1.0);
        var actual = new IncidentAnalysisResult
        {
            IncidentType = "Flood",
            RecommendedDepartment = "Rescue",
            Facts = new EmergencyFacts { PeopleTrapped = true, RoadBlocked = true, ImmediateDanger = false },
            PeopleAffected = 4,
            VehiclesAffected = 2,
            Casualties = 0,
            Confidence = 0.8
        };

        var score = new IncidentAnalysisEvaluator().Evaluate(expected, actual);

        Assert.True(score.IncidentTypeCorrect);
        Assert.Equal(2.0 / 3.0, score.CriticalFactRecall, 8);
        Assert.True(score.ConfidenceInRange);
    }

    [Fact]
    public void RegressionPolicy_FailsCriticalMetricDropBeyondThreshold()
    {
        var policy = new RegressionPolicy { MaximumMetricDrop = 0.02 };
        var baseline = new Dictionary<string, double>
        {
            ["CriticalFactRecall"] = 0.98,
            ["Relevance"] = 0.90
        };
        var current = new Dictionary<string, double>
        {
            ["CriticalFactRecall"] = 0.95,
            ["Relevance"] = 0.70
        };

        var regressions = policy.Compare(baseline, current);

        Assert.Single(regressions);
        Assert.Contains("CriticalFactRecall", regressions[0], StringComparison.Ordinal);
    }

    [Fact]
    public void VideoHazardPreservationEvaluator_PreservesAnyCriticalFrameFinding()
    {
        var result = new EmergencyResponse.Application.AI.VisualAnalysis.VisualAnalysisResult
        {
            FloodingVisible = true,
            FrameFindings =
            [
                new EmergencyResponse.Application.AI.VisualAnalysis.VisualFrameFinding
                {
                    FrameNumber = 1,
                    FloodingVisible = true
                }
            ]
        };

        Assert.True(new VideoHazardPreservationEvaluator().Evaluate(result));
        Assert.False(new VideoHazardPreservationEvaluator().Evaluate(result with { FloodingVisible = false }));
    }
}
