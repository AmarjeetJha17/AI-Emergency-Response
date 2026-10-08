using System.Text.Json;
using System.Net;
using System.Text;

namespace EmergencyResponse.CopilotEvaluation.Infrastructure;

public sealed class EvaluationResultWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<string> WriteAsync<TCase>(
        string reportsRoot,
        EvaluationRunResult result,
        IReadOnlyList<TCase> cases,
        CancellationToken cancellationToken = default)
    {
        var outputDirectory = Path.Combine(
            reportsRoot,
            DateTimeOffset.UtcNow.ToString("yyyy-MM-dd"),
            result.RunId);
        Directory.CreateDirectory(outputDirectory);

        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "summary.json"),
            JsonSerializer.Serialize(result, JsonOptions),
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "cases.jsonl"),
            string.Join(Environment.NewLine, cases.Select(item => JsonSerializer.Serialize(item))) + Environment.NewLine,
            cancellationToken);
        await File.WriteAllTextAsync(
            Path.Combine(outputDirectory, "metrics.json"),
            JsonSerializer.Serialize(result.Metrics, JsonOptions),
            cancellationToken);
        await WriteHtmlReportAsync(outputDirectory, result, cancellationToken);
        return outputDirectory;
    }

    public Task WriteHtmlReportAsync(
        string outputDirectory,
        EvaluationRunResult result,
        CancellationToken cancellationToken = default)
        => File.WriteAllTextAsync(Path.Combine(outputDirectory, "report.html"), CreateHtmlReport(result), cancellationToken);

    private static string CreateHtmlReport(EvaluationRunResult result)
    {
        var metrics = string.Join(Environment.NewLine, result.Metrics.OrderBy(x => x.Key)
            .Select(metric => $"<tr><th>{WebUtility.HtmlEncode(metric.Key)}</th><td>{metric.Value:0.####}</td></tr>"));
        var failed = result.FailedCaseIds.Count == 0
            ? "<p>No failed cases or gates.</p>"
            : $"<ul>{string.Join(Environment.NewLine, result.FailedCaseIds.Select(id => $"<li>{WebUtility.HtmlEncode(id)}</li>"))}</ul>";
        var html = new StringBuilder();
        html.AppendLine("<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>AI Evaluation Report</title>");
        html.AppendLine("<style>body{font:16px system-ui;max-width:900px;margin:2rem auto;padding:0 1rem;color:#17202a}table{border-collapse:collapse;width:100%}th,td{text-align:left;padding:.6rem;border-bottom:1px solid #ddd}.status{font-weight:bold}</style><body>");
        html.AppendLine($"<h1>AI Evaluation Report</h1><p class=\"status\">Status: {WebUtility.HtmlEncode(result.OverallStatus)}</p>");
        html.AppendLine($"<dl><dt>Run</dt><dd>{WebUtility.HtmlEncode(result.RunId)}</dd><dt>Mode</dt><dd>{WebUtility.HtmlEncode(result.Mode)}</dd><dt>Dataset</dt><dd>{WebUtility.HtmlEncode(result.DatasetVersion)}</dd><dt>Model</dt><dd>{WebUtility.HtmlEncode(result.Model ?? "Not applicable")}</dd><dt>Prompt version</dt><dd>{WebUtility.HtmlEncode(result.PromptVersion ?? "Not applicable")}</dd><dt>Feature schema</dt><dd>{WebUtility.HtmlEncode(result.FeatureSchemaVersion ?? "Not applicable")}</dd><dt>Cases</dt><dd>{result.PassedCases}/{result.TotalCases} passed</dd></dl>");
        html.AppendLine($"<h2>Metrics</h2><table><thead><tr><th>Metric</th><th>Value</th></tr></thead><tbody>{metrics}</tbody></table><h2>Failed cases and gates</h2>{failed}</body></html>");
        return html.ToString();
    }
}
