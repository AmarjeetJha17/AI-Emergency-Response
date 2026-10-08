using System.Text.Json;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.VisualAnalysis;
using EmergencyResponse.CopilotEvaluation.Evaluators;
using EmergencyResponse.CopilotEvaluation.Infrastructure;

var command = args.FirstOrDefault(value => !value.StartsWith("--", StringComparison.Ordinal))?.ToLowerInvariant() ?? "all";
if (args.Contains("--help", StringComparer.OrdinalIgnoreCase) || command == "help")
{
    Console.WriteLine("Usage: dotnet run --project tests/EmergencyResponse.CopilotEvaluation -- [incident-analysis|visual|video|duplicate-detection|situation-summary|copilot|rag|security|predictions|all] [--live] [--compare <approved-summary.json>] [--update-baseline]");
    return 0;
}

var supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "incident-analysis", "visual", "video", "duplicate-detection", "situation-summary",
    "copilot", "rag", "security", "predictions", "all"
};
if (!supported.Contains(command))
{
    Console.Error.WriteLine($"Unknown evaluation dataset '{command}'. Use --help for the supported names.");
    return 2;
}

try
{
    var projectRoot = FindProjectRoot(AppContext.BaseDirectory);
    var dataRoot = Path.Combine(projectRoot, "Data");
    var datasetRoot = Path.Combine(projectRoot, "Datasets");
    var reportsRoot = Path.Combine(projectRoot, "Reports");
    var thresholds = await EvaluationThresholds.LoadAsync(
        Path.Combine(projectRoot, "EvaluationThresholds.json"));
    var loader = new EvaluationCaseLoader();
    var manifestPath = Path.Combine(datasetRoot, "dataset-manifest.json");
    var manifest = await loader.LoadManifestAsync(manifestPath);
    var incidentCases = await loader.LoadAsync<IncidentAnalysisInput, IncidentExpectedFacts>(
        Path.Combine(datasetRoot, "IncidentAnalysis"), manifest.DatasetVersion);
    var visualCases = await loader.LoadAsync<VisualAnalysisInput, VisualExpectedFacts>(
        Path.Combine(datasetRoot, "VisualAnalysis"), manifest.DatasetVersion);
    VerifyCount(manifest, "incidentAnalysis", incidentCases.Count);
    VerifyCount(manifest, "visualAnalysis", visualCases.Count);
    var videoDataset = Path.Combine(datasetRoot, "VisualAnalysis", "VideoBenchmark");
    var videoCases = await loader.LoadAsync<VideoVisualInput, VisualExpectedFacts>(videoDataset, manifest.DatasetVersion);
    VerifyCount(manifest, "videoAnalysis", videoCases.Count);
    VerifyCount(manifest, "rag", await loader.CountCasesAsync(Path.Combine(datasetRoot, "RAG")));
    VerifyCount(manifest, "copilot", await loader.CountCasesAsync(Path.Combine(datasetRoot, "Copilot")));
    VerifyCount(manifest, "security", await loader.CountCasesAsync(Path.Combine(datasetRoot, "Security")));
    VerifyCount(manifest, "duplicateDetection", await CountJsonArrayItemsAsync(
        Path.Combine(datasetRoot, "DuplicateDetection", "boundary-cases.json"), "scoreRange"));
    VerifyCount(manifest, "situationSummary", 1);
    VerifyCount(manifest, "predictions", 1);
    foreach (var visualCase in visualCases)
    {
        var asset = Path.GetFullPath(Path.Combine(datasetRoot, "VisualAnalysis", visualCase.Input.AssetPath));
        if (!asset.StartsWith(Path.GetFullPath(Path.Combine(datasetRoot, "VisualAnalysis")), StringComparison.OrdinalIgnoreCase) ||
            !File.Exists(asset))
        {
            throw new InvalidDataException($"Visual dataset case '{visualCase.Id}' has a missing or unsafe asset path.");
        }
    }
    var visualAssetRoot = Path.GetFullPath(Path.Combine(datasetRoot, "VisualAnalysis"));
    foreach (var videoCase in videoCases)
    {
        foreach (var frame in videoCase.Input.Frames)
        {
            var asset = Path.GetFullPath(Path.Combine(videoDataset, frame.AssetPath));
            if (!asset.StartsWith(visualAssetRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(asset))
            {
                throw new InvalidDataException($"Video dataset case '{videoCase.Id}' has a missing or unsafe frame path.");
            }
        }
    }
    var runner = new EvaluationRunner();

    (EvaluationRunResult Result, string OutputPath) run;
    if (args.Contains("--live", StringComparer.OrdinalIgnoreCase))
    {
        if (command == "all")
        {
            var runs = new List<(EvaluationRunResult Result, string OutputPath)>
            {
                await runner.RunLiveIncidentAnalysisAsync(
                    Path.Combine(datasetRoot, "IncidentAnalysis"), manifestPath, reportsRoot, thresholds),
                await runner.RunLiveVisualAnalysisAsync(
                    Path.Combine(datasetRoot, "VisualAnalysis"), manifestPath, reportsRoot, thresholds),
                await runner.RunLiveVideoAnalysisAsync(
                    videoDataset, manifestPath, reportsRoot, thresholds),
                await runner.RunLiveQualityAndSafetyAsync(
                    Path.Combine(dataRoot, "live-scenarios.jsonl"), reportsRoot,
                    Path.Combine(reportsRoot, "cache"), thresholds, manifest.DatasetVersion)
            };
            var mergedMetrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (var suite in runs)
            {
                foreach (var metric in suite.Result.Metrics)
                {
                    mergedMetrics[$"{suite.Result.Mode}.{metric.Key}"] = metric.Value;
                }
            }

            var failedIds = runs.SelectMany(suite => suite.Result.FailedCaseIds
                .Select(id => $"{suite.Result.Mode}:{id}")).ToArray();
            var startedAt = runs.Min(suite => suite.Result.StartedAtUtc);
            var completedAt = runs.Max(suite => suite.Result.CompletedAtUtc);
            var combined = new EvaluationRunResult
            {
                RunId = $"eval-{DateTimeOffset.UtcNow:yyyyMMddTHHmmss}-{Guid.NewGuid():N}",
                DatasetVersion = manifest.DatasetVersion,
                Mode = "live-full-suite",
                Model = Environment.GetEnvironmentVariable("AI_EVAL_CANDIDATE_MODEL") ?? "configured-candidate",
                Deployment = Environment.GetEnvironmentVariable("AI_EVAL_CANDIDATE_DEPLOYMENT"),
                PromptVersion = Environment.GetEnvironmentVariable("AI_EVAL_PROMPT_VERSION") ?? "unversioned",
                FeatureSchemaVersion = Environment.GetEnvironmentVariable("AI_EVAL_FEATURE_SCHEMA_VERSION") ?? "multiple",
                StartedAtUtc = startedAt,
                CompletedAtUtc = completedAt,
                TotalCases = runs.Sum(suite => suite.Result.TotalCases),
                PassedCases = runs.Sum(suite => suite.Result.PassedCases),
                FailedCases = runs.Sum(suite => suite.Result.FailedCases),
                OverallStatus = runs.All(suite => suite.Result.OverallStatus == "Passed") ? "Passed" : "Failed",
                Metrics = mergedMetrics,
                FailedCaseIds = failedIds
            };
            var output = await new EvaluationResultWriter().WriteAsync(
                reportsRoot, combined, runs.Select(suite => suite.Result).ToArray());
            run = (combined, output);
        }
        else if (command == "incident-analysis")
        {
            run = await runner.RunLiveIncidentAnalysisAsync(
                Path.Combine(datasetRoot, "IncidentAnalysis"),
                Path.Combine(datasetRoot, "dataset-manifest.json"),
                reportsRoot,
                thresholds);
        }
        else if (command == "visual")
        {
            run = await runner.RunLiveVisualAnalysisAsync(
                Path.Combine(datasetRoot, "VisualAnalysis"),
                Path.Combine(datasetRoot, "dataset-manifest.json"),
                reportsRoot,
                thresholds);
        }
        else if (command == "video")
        {
            run = await runner.RunLiveVideoAnalysisAsync(videoDataset, manifestPath, reportsRoot, thresholds);
        }
        else
        {
            var category = command switch
            {
                "copilot" => "copilot",
                "rag" => "rag",
                "security" => "security",
                _ => throw new ArgumentException(
                    $"Live evaluation for '{command}' is not implemented. Choose incident-analysis, visual, video, copilot, rag, security, or all.")
            };
            var cachePath = Path.Combine(reportsRoot, "cache");
            run = await runner.RunLiveQualityAndSafetyAsync(
                Path.Combine(dataRoot, "live-scenarios.jsonl"),
                reportsRoot,
                cachePath,
                thresholds,
                manifest.DatasetVersion,
                category);
        }
    }
    else
    {
        run = await runner.RunOfflineAsync(
            dataRoot,
            reportsRoot,
            thresholds,
            manifest.DatasetVersion);
    }

    var failures = new List<string>(run.Result.FailedCaseIds);
    var compareIndex = Array.FindIndex(args, value => value.Equals("--compare", StringComparison.OrdinalIgnoreCase));
    if (compareIndex >= 0)
    {
        if (compareIndex + 1 >= args.Length)
        {
            throw new ArgumentException("--compare requires an approved baseline JSON path.");
        }

        var baselinePath = Path.GetFullPath(args[compareIndex + 1]);
        var baseline = await JsonSerializer.DeserializeAsync<EvaluationRunResult>(
            File.OpenRead(baselinePath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException($"Could not read baseline '{baselinePath}'.");
        failures.AddRange(new RegressionPolicy().Compare(baseline.Metrics, run.Result.Metrics));
    }

    var final = run.Result with
    {
        OverallStatus = failures.Count == 0 ? run.Result.OverallStatus : "Failed",
        FailedCaseIds = failures.Distinct().ToArray()
    };
    await new EvaluationResultWriter().WriteHtmlReportAsync(run.OutputPath, final);
    var summaryPath = Path.Combine(run.OutputPath, "summary.json");
    await File.WriteAllTextAsync(summaryPath, JsonSerializer.Serialize(final, new JsonSerializerOptions
    {
        WriteIndented = true
    }));

    if (args.Contains("--update-baseline", StringComparer.OrdinalIgnoreCase))
    {
        if (final.OverallStatus != "Passed")
        {
            throw new InvalidOperationException("Refusing to set a baseline from a failed evaluation run.");
        }

        await File.WriteAllTextAsync(
            Path.Combine(reportsRoot, "approved-baseline.json"),
            JsonSerializer.Serialize(final, new JsonSerializerOptions { WriteIndented = true }));
    }

    Console.WriteLine(JsonSerializer.Serialize(final, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Report: {run.OutputPath}");
    return final.OverallStatus == "Failed" ? 1 : 0;
}
catch (Exception exception)
{
    Console.Error.WriteLine($"Evaluation failed: {exception.Message}");
    return 1;
}

static async Task<int> CountJsonArrayItemsAsync(string path, string property)
{
    await using var stream = File.OpenRead(path);
    using var document = await JsonDocument.ParseAsync(stream);
    return document.RootElement.GetProperty(property).GetArrayLength();
}

static void VerifyCount(EvaluationDatasetManifest manifest, string dataset, int actual)
{
    if (!manifest.Cases.TryGetValue(dataset, out var expected) || expected != actual)
    {
        throw new InvalidDataException(
            $"Dataset manifest count for '{dataset}' does not match: expected {manifest.Cases.GetValueOrDefault(dataset)}, found {actual}.");
    }
}

static string FindProjectRoot(string start)
{
    var directory = new DirectoryInfo(start);
    while (directory is not null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "EmergencyResponse.CopilotEvaluation.csproj")))
        {
            return directory.FullName;
        }

        directory = directory.Parent;
    }

    throw new DirectoryNotFoundException("Could not find the CopilotEvaluation project root.");
}
