using System.Text.Json;

namespace EmergencyResponse.CopilotEvaluation.Infrastructure;

public sealed class EvaluationCase<TInput, TExpected>
{
    public string Id { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public TInput Input { get; init; } = default!;
    public TExpected Expected { get; init; } = default!;
    public string Category { get; init; } = string.Empty;
    public string DatasetVersion { get; init; } = string.Empty;
    public JsonElement? CandidateOutput { get; init; }
}

public sealed class EvaluationDatasetManifest
{
    public string DatasetVersion { get; init; } = string.Empty;
    public string SchemaVersion { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public Dictionary<string, int> Cases { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class BinaryMetric
{
    public int TruePositive { get; init; }
    public int TrueNegative { get; init; }
    public int FalsePositive { get; init; }
    public int FalseNegative { get; init; }

    public double Precision => Divide(TruePositive, TruePositive + FalsePositive);
    public double Recall => Divide(TruePositive, TruePositive + FalseNegative);
    public double F1 => Precision + Recall == 0 ? 0 : 2 * Precision * Recall / (Precision + Recall);
    public int Total => TruePositive + TrueNegative + FalsePositive + FalseNegative;

    public static BinaryMetric From(IReadOnlyList<(bool Expected, bool Actual)> rows)
        => new()
        {
            TruePositive = rows.Count(x => x.Expected && x.Actual),
            TrueNegative = rows.Count(x => !x.Expected && !x.Actual),
            FalsePositive = rows.Count(x => !x.Expected && x.Actual),
            FalseNegative = rows.Count(x => x.Expected && !x.Actual)
        };

    private static double Divide(int numerator, int denominator)
        => denominator == 0 ? 0 : (double)numerator / denominator;
}

public sealed record EvaluationRunResult
{
    public string RunId { get; init; } = string.Empty;
    public string DatasetVersion { get; init; } = string.Empty;
    public string EvaluationVersion { get; init; } = "eval-v1";
    public string Mode { get; init; } = "offline";
    public string? Model { get; init; }
    public string? Deployment { get; init; }
    public string? PromptVersion { get; init; }
    public string? FeatureSchemaVersion { get; init; }
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset CompletedAtUtc { get; init; }
    public int TotalCases { get; init; }
    public int PassedCases { get; init; }
    public int FailedCases { get; init; }
    public string OverallStatus { get; init; } = "Passed";
    public IReadOnlyDictionary<string, double> Metrics { get; init; } = new Dictionary<string, double>();
    public IReadOnlyList<string> FailedCaseIds { get; init; } = [];
}
