using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using EmergencyResponse.LoadTests.Infrastructure;
using EmergencyResponse.LoadTests.Scenarios;
using EmergencyResponse.LoadTests.Data;
using NBomber.CSharp;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using Microsoft.Extensions.DependencyInjection;

namespace EmergencyResponse.LoadTests;

internal static class LoadTestProgram
{
    public static async Task<int> Main(string[] args)
    {
        var options = LoadTestOptions.Load(args);
        if (string.Equals(options.HostMode, "External", StringComparison.OrdinalIgnoreCase))
        {
            EnsureExternalTargetIsNonProduction(options.BaseUrl);
        }

        LoadTestHostFactory? localHost = null;
        try
        {
            HttpClient client;
            LoadTestDatabaseSnapshot data;
            string accessToken;
            Func<HttpMessageHandler>? signalRHandlerFactory = null;

            if (string.Equals(options.HostMode, "LocalTestHost", StringComparison.OrdinalIgnoreCase))
            {
                localHost = new LoadTestHostFactory(options);
                await localHost.InitializeAsync();
                client = localHost.CreateClient();
                data = localHost.Data;
                accessToken = localHost.AccessToken;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                signalRHandlerFactory = localHost.Server.CreateHandler;
            }
            else if (string.Equals(options.HostMode, "External", StringComparison.OrdinalIgnoreCase))
            {
                client = new HttpClient { BaseAddress = new Uri(options.BaseUrl), Timeout = TimeSpan.FromSeconds(60) };
                accessToken = options.BearerToken ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(accessToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                data = ExternalFixtures(options);
                if (!string.Equals(options.Scenario, "health", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new InvalidOperationException("External API scenarios require BEARER_TOKEN for a dedicated test identity.");
                }
            }
            else
            {
                throw new InvalidOperationException("LOAD_TEST_HOST_MODE must be LocalTestHost or External.");
            }

            var metrics = new ScenarioMetrics();
            var faults = localHost?.Faults ?? new LoadTestFaultController();
            faults.ResetSqlMetrics();
            var scenarioContext = new LoadScenarioContext(client, options, data, metrics, faults, accessToken, signalRHandlerFactory);
            var runId = ActivityTraceId.CreateRandom().ToString();
            var reportDirectory = Path.GetFullPath(Path.Combine(options.ReportDirectory, runId));
            Directory.CreateDirectory(reportDirectory);
            EnsureFixtureRequirements(options, data);

            if (string.Equals(options.Scenario, "queue", StringComparison.OrdinalIgnoreCase))
            {
                RequireLocalHost(localHost, "Queue pressure/recovery");
                var queueReport = await QueuePressureRunner.RunAsync(localHost!, options, CancellationToken.None);
                await WriteJsonAsync(Path.Combine(reportDirectory, "run-summary.json"), new
                {
                    runId,
                    environment = "LocalTestHost / single-machine development baseline",
                    buildVersion = BuildVersion(),
                    dataset = options.Dataset,
                    profile = options.Profile,
                    durationSeconds = options.TargetDurationSeconds,
                    queue = queueReport
                });
                Console.WriteLine(JsonSerializer.Serialize(queueReport, JsonOptions));
                return QueueRecovered(queueReport) ? 0 : 2;
            }

            if (string.Equals(options.Scenario, "worker", StringComparison.OrdinalIgnoreCase))
            {
                RequireLocalHost(localHost, "Worker saturation");
                var workerReport = await WorkerSaturationRunner.RunAsync(localHost!, options, CancellationToken.None);
                await WriteJsonAsync(Path.Combine(reportDirectory, "run-summary.json"), new
                {
                    runId,
                    environment = "LocalTestHost / single-machine development baseline",
                    buildVersion = BuildVersion(),
                    dataset = options.Dataset,
                    sql = faults.SqlSnapshot(),
                    worker = workerReport
                });
                Console.WriteLine(JsonSerializer.Serialize(workerReport, JsonOptions));
                return WorkerRecovered(workerReport) ? 0 : 2;
            }

            if (string.Equals(options.Scenario, "rate-limit", StringComparison.OrdinalIgnoreCase))
            {
                RequireLocalHost(localHost, "Rate-limit verification");
                var rateLimitReport = await RateLimitScenario.RunAsync(localHost!, scenarioContext, CancellationToken.None);
                await WriteJsonAsync(Path.Combine(reportDirectory, "run-summary.json"), new
                {
                    runId,
                    environment = "LocalTestHost / single-machine development baseline",
                    buildVersion = BuildVersion(),
                    dataset = options.Dataset,
                    rateLimit = rateLimitReport
                });
                Console.WriteLine(JsonSerializer.Serialize(rateLimitReport, JsonOptions));
                using var rateJson = JsonDocument.Parse(JsonSerializer.Serialize(rateLimitReport));
                return rateJson.RootElement.GetProperty("passed").GetBoolean() ? 0 : 2;
            }

            if (string.Equals(options.Scenario, "chaos", StringComparison.OrdinalIgnoreCase))
            {
                RequireLocalHost(localHost, "Dependency chaos");
                var chaosReport = await ChaosScenario.RunAsync(scenarioContext, localHost!.Services, CancellationToken.None);
                await WriteJsonAsync(Path.Combine(reportDirectory, "run-summary.json"), new
                {
                    runId,
                    environment = "LocalTestHost / single-machine development baseline",
                    buildVersion = BuildVersion(),
                    dataset = options.Dataset,
                    profile = options.Profile,
                    chaos = chaosReport,
                    sql = faults.SqlSnapshot()
                });
                Console.WriteLine(JsonSerializer.Serialize(chaosReport, JsonOptions));
                return ChaosPassed(chaosReport) ? 0 : 2;
            }

            var scenario = SelectScenario(options.Scenario, scenarioContext);
            var configured = LoadProfiles.Apply(scenario, options.Profile, options.WarmupSeconds,
                options.IncidentCreateRequestsPerSecond, options.CopilotRequestsPerSecond,
                options.SignalRConnectionCount, options.TargetDurationSeconds);
            var runner = NBomberRunner.RegisterScenarios(configured)
                .WithReportFolder(reportDirectory)
                .WithReportFormats(ReportFormat.Txt, ReportFormat.Html, ReportFormat.Csv, ReportFormat.Md);

            using var process = Process.GetCurrentProcess();
            var workingSetBefore = process.WorkingSet64;
            var gcHeapBefore = GC.GetTotalMemory(forceFullCollection: false);
            await using var resourceSampler = ProcessResourceSampler.Start();
            var runTimer = Stopwatch.StartNew();
            runner.Run();
            runTimer.Stop();
            await resourceSampler.StopAsync();
            process.Refresh();

            object? mediaCleanup = null;
            if (string.Equals(options.Scenario, "media", StringComparison.OrdinalIgnoreCase) && localHost is not null)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
                var storage = localHost.Services.GetRequiredService<LoadTestMediaStorage>();
                var filesBeforeCleanup = storage.TemporaryFileCount;
                storage.Cleanup();
                mediaCleanup = new
                {
                    filesBeforeCleanup,
                    filesAfterCleanup = storage.TemporaryFileCount,
                    temporaryDirectoryRemoved = !Directory.Exists(storage.TemporaryDirectory)
                };
            }

            var summary = new
            {
                runId,
                environment = string.Equals(options.HostMode, "LocalTestHost", StringComparison.OrdinalIgnoreCase)
                    ? "LocalTestHost / single-machine development baseline (in-process HTTP; not production capacity)"
                    : "External non-production target",
                buildVersion = BuildVersion(),
                baseUrl = options.HostMode == "External" ? new Uri(options.BaseUrl).GetLeftPart(UriPartial.Authority) : "TestServer",
                dataset = options.Dataset,
                geography = options.GeographicDistribution,
                scenario = options.Scenario,
                profile = options.Profile,
                warmupSeconds = options.WarmupSeconds,
                configuredDurationSeconds = options.TargetDurationSeconds,
                actualDurationSeconds = Math.Round(runTimer.Elapsed.TotalSeconds, 2),
                requests = metrics.TotalRequests,
                failures = metrics.TotalFailures,
                errorRate = metrics.ErrorRate,
                maximumErrorRate = options.MaximumErrorRate,
                maximumRelativeLatencyIncrease = options.MaximumRelativeLatencyIncrease,
                processWorkingSetBeforeBytes = workingSetBefore,
                processWorkingSetAfterBytes = process.WorkingSet64,
                managedHeapBeforeBytes = gcHeapBefore,
                managedHeapAfterBytes = GC.GetTotalMemory(forceFullCollection: false),
                processResources = resourceSampler.Snapshot(),
                sql = faults.SqlSnapshot(),
                routeMetrics = metrics.Snapshot(),
                mediaCleanup
            };

            object? regression = null;
            if (!string.IsNullOrWhiteSpace(options.BaselineReportPath))
            {
                using var current = JsonDocument.Parse(JsonSerializer.Serialize(summary));
                regression = PerformanceRegressionPolicy.Compare(options.BaselineReportPath,
                    current.RootElement, options.MaximumRelativeLatencyIncrease, options.MaximumErrorRate);
            }

            await WriteJsonAsync(Path.Combine(reportDirectory, "run-summary.json"), new { run = summary, regression });
            Console.WriteLine($"Custom request metrics: {metrics.TotalRequests} requests, {metrics.TotalFailures} failures ({metrics.ErrorRate:P2}).");
            Console.WriteLine($"Reports: {reportDirectory}");
            return metrics.ErrorRate > options.MaximumErrorRate ||
                   mediaCleanup is not null && !MediaCleanupPassed(mediaCleanup) ||
                   (options.EnforceRegression && regression is not null && !RegressionPassed(regression)) ? 2 : 0;
        }
        finally
        {
            localHost?.Dispose();
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static ScenarioProps SelectScenario(string name, LoadScenarioContext context) => name.ToLowerInvariant() switch
    {
        "health" => HealthScenario.Create(context),
        "incident-read" => IncidentReadScenario.Create(context),
        "incident-detail" => IncidentReadScenario.CreateDetails(context),
        "incident-create" => IncidentCreateScenario.Create(context),
        "dashboard" => DashboardScenario.Create(context),
        "dashboard-combined" => DashboardScenario.CreateCombined(context),
        "map" => MapScenario.Create(context),
        "resource" => ResourceScenario.Create(context),
        "situation" => SituationScenario.Create(context),
        "notification" => NotificationScenario.Create(context),
        "copilot" => CopilotScenario.Create(context),
        "copilot-real" => RealCopilotScenario.Create(context),
        "signalr" => SignalRScenario.CreateConnections(context),
        "signalr-delivery" => SignalRScenario.CreateDelivery(context),
        "resource-concurrency" => ResourceConcurrencyScenario.Create(context),
        "media" => MediaUploadScenario.Create(context),
        "mixed" => MixedOperationsScenario.Create(context),
        _ => throw new InvalidOperationException("Unknown scenario. Choose health, incident-read, incident-detail, incident-create, dashboard, dashboard-combined, map, resource, resource-concurrency, situation, notification, media, copilot, copilot-real, signalr, signalr-delivery, mixed, chaos, queue, worker, or rate-limit.")
    };

    private static LoadTestDatabaseSnapshot ExternalFixtures(LoadTestOptions options)
    {
        var incidentIds = ParseGuid(options.IncidentId);
        var resourceIds = ParseGuid(options.ResourceId);
        var notificationIds = ParseGuid(options.NotificationId);
        return new LoadTestDatabaseSnapshot(incidentIds, [], resourceIds, notificationIds, Guid.Empty, "");
    }

    private static IReadOnlyList<Guid> ParseGuid(string? value) => Guid.TryParse(value, out var id) ? [id] : [];

    private static void EnsureFixtureRequirements(LoadTestOptions options, LoadTestDatabaseSnapshot data)
    {
        var scenario = options.Scenario.ToLowerInvariant();
        if (options.HostMode.Equals("External", StringComparison.OrdinalIgnoreCase) &&
            (scenario is "incident-detail" or "mixed") && data.IncidentIds.Count == 0)
        {
            throw new InvalidOperationException("Set LOAD_TEST_INCIDENT_ID to a valid incident in the dedicated non-production dataset for this scenario.");
        }

        if (options.HostMode.Equals("External", StringComparison.OrdinalIgnoreCase) &&
            scenario == "resource" && data.ResourceIds.Count == 0)
        {
            throw new InvalidOperationException("Set LOAD_TEST_RESOURCE_ID to a valid resource in the dedicated non-production dataset.");
        }

        if (options.HostMode.Equals("External", StringComparison.OrdinalIgnoreCase) &&
            scenario == "notification" && data.NotificationIds.Count == 0)
        {
            throw new InvalidOperationException("Set LOAD_TEST_NOTIFICATION_ID to a notification owned by the dedicated external test identity.");
        }

        if (options.HostMode.Equals("External", StringComparison.OrdinalIgnoreCase) &&
            scenario == "resource-concurrency")
        {
            throw new InvalidOperationException("Resource concurrency writes are restricted to LocalTestHost.");
        }

        if (options.HostMode.Equals("External", StringComparison.OrdinalIgnoreCase) &&
            (scenario is "incident-create" or "signalr-delivery" or "media"))
        {
            throw new InvalidOperationException("Write-heavy or event-delivery scenarios are restricted to the isolated LocalTestHost.");
        }

        if (options.HostMode.Equals("External", StringComparison.OrdinalIgnoreCase) && scenario == "copilot")
        {
            throw new InvalidOperationException("External Copilot requests may invoke paid providers. Use copilot-real with an explicit request cap and cost estimate.");
        }
    }

    private static void EnsureExternalTargetIsNonProduction(string baseUrl)
    {
        var uri = new Uri(baseUrl);
        var host = uri.Host;
        var clearlyNonProduction = uri.IsLoopback || host.Contains("staging", StringComparison.OrdinalIgnoreCase) ||
                                   host.Contains("stage", StringComparison.OrdinalIgnoreCase) ||
                                   host.Contains("dev", StringComparison.OrdinalIgnoreCase) ||
                                   host.EndsWith(".test", StringComparison.OrdinalIgnoreCase);
        if (!clearlyNonProduction && !string.Equals(Environment.GetEnvironmentVariable("LOAD_TEST_NONPROD_TARGET_CONFIRMED"), "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("External target hostname is not recognizable as non-production. Confirm a dedicated staging target with LOAD_TEST_NONPROD_TARGET_CONFIRMED=true.");
        }
    }

    private static void RequireLocalHost(LoadTestHostFactory? host, string action)
    {
        if (host is null)
        {
            throw new InvalidOperationException($"{action} currently requires LocalTestHost with a dedicated ConnectionStrings__LoadTestDatabase.");
        }
    }

    private static Task WriteJsonAsync(string path, object value) =>
        File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, JsonOptions));

    private static bool QueueRecovered(object report)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(report));
        return json.RootElement.GetProperty("recovered").GetBoolean();
    }

    private static bool WorkerRecovered(object report)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(report));
        return json.RootElement.GetProperty("recovered").GetBoolean();
    }

    private static bool ChaosPassed(object report)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(report));
        return json.RootElement.GetProperty("failed").GetInt32() == 0;
    }

    private static bool RegressionPassed(object report)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(report));
        return json.RootElement.GetProperty("passed").GetBoolean();
    }

    private static bool MediaCleanupPassed(object report)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(report));
        return json.RootElement.GetProperty("filesAfterCleanup").GetInt32() == 0 &&
               json.RootElement.GetProperty("temporaryDirectoryRemoved").GetBoolean();
    }

    private static string BuildVersion() => Environment.GetEnvironmentVariable("BUILD_VERSION") ??
                                            typeof(LoadTestProgram).Assembly.GetName().Version?.ToString() ?? "unknown";
}
