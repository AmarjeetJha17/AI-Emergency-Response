using System.Text.Json;

namespace EmergencyResponse.LoadTests.Infrastructure;

public static class PerformanceRegressionPolicy
{
    public static object Compare(string baselinePath, JsonElement current, double maximumLatencyIncrease, double maximumErrorRate)
    {
        using var baselineDocument = JsonDocument.Parse(File.ReadAllText(baselinePath));
        var baseline = Unwrap(baselineDocument.RootElement);
        var findings = new List<RegressionFinding>();
        var baselineMetrics = ReadMetrics(baseline);
        var currentMetrics = ReadMetrics(current);
        foreach (var pair in currentMetrics)
        {
            if (!baselineMetrics.TryGetValue(pair.Key, out var previous) || previous.P95Ms <= 0 || pair.Value.P95Ms <= 0)
            {
                continue;
            }

            var increase = pair.Value.P95Ms / previous.P95Ms - 1;
            if (increase > maximumLatencyIncrease)
            {
                findings.Add(new RegressionFinding(pair.Key, previous.P95Ms, pair.Value.P95Ms,
                    Math.Round(increase * 100, 2), $"P95 latency increased by more than {maximumLatencyIncrease:P0}."));
            }
        }

        current = Unwrap(current);
        var errorRate = current.TryGetProperty("errorRate", out var error) ? error.GetDouble() : 0;
        return new
        {
            baselinePath = Path.GetFileName(baselinePath),
            maximumLatencyIncrease,
            maximumErrorRate,
            errorRate,
            routeComparisons = currentMetrics.Count,
            findings,
            passed = findings.Count == 0 && errorRate <= maximumErrorRate
        };
    }

    private static Dictionary<string, Metric> ReadMetrics(JsonElement document)
    {
        var result = new Dictionary<string, Metric>(StringComparer.OrdinalIgnoreCase);
        document = Unwrap(document);
        if (!document.TryGetProperty("routeMetrics", out var routes) || routes.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var route in routes.EnumerateArray())
        {
            if (route.TryGetProperty("scenario", out var name) &&
                route.TryGetProperty("p95Ms", out var p95) &&
                name.GetString() is { } key)
            {
                result[key] = new Metric(p95.GetDouble());
            }
        }

        return result;
    }

    private static JsonElement Unwrap(JsonElement document) =>
        document.TryGetProperty("run", out var run) && run.ValueKind == JsonValueKind.Object ? run : document;

    private sealed record Metric(double P95Ms);
    private sealed record RegressionFinding(string Scenario, double BaselineP95Ms, double CurrentP95Ms, double IncreasePercent, string Reason);
}
