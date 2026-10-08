namespace EmergencyResponse.CopilotEvaluation.Evaluators;

public sealed class ToolSelectionEvaluator
{
    public ToolSelectionScore Evaluate(
        IReadOnlySet<string> expectedTools,
        IReadOnlySet<string> actualTools,
        IReadOnlySet<string> forbiddenTools,
        IReadOnlyDictionary<string, string>? expectedArguments = null,
        IReadOnlyDictionary<string, string>? actualArguments = null)
    {
        var truePositive = expectedTools.Intersect(actualTools, StringComparer.OrdinalIgnoreCase).Count();
        var recall = expectedTools.Count == 0 ? 1 : (double)truePositive / expectedTools.Count;
        var precision = actualTools.Count == 0 ? (expectedTools.Count == 0 ? 1 : 0) : (double)truePositive / actualTools.Count;
        var forbiddenSelected = actualTools.Overlaps(forbiddenTools);
        var argsExpected = expectedArguments ?? new Dictionary<string, string>();
        var argsActual = actualArguments ?? new Dictionary<string, string>();
        var matchingArguments = argsExpected.Count == 0
            ? 1
            : (double)argsExpected.Count(pair => argsActual.TryGetValue(pair.Key, out var actual) &&
                string.Equals(pair.Value, actual, StringComparison.OrdinalIgnoreCase)) / argsExpected.Count;

        return new ToolSelectionScore
        {
            Recall = recall,
            Precision = precision,
            ArgumentAccuracy = matchingArguments,
            ForbiddenToolSelected = forbiddenSelected,
            Passed = recall == 1 && matchingArguments == 1 && !forbiddenSelected
        };
    }
}

public sealed class ToolSelectionScore
{
    public double Recall { get; init; }
    public double Precision { get; init; }
    public double ArgumentAccuracy { get; init; }
    public bool ForbiddenToolSelected { get; init; }
    public bool Passed { get; init; }
}
