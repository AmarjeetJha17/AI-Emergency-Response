using System.Globalization;
using System.Text.Json;

namespace EmergencyResponse.LoadTests.Infrastructure;

public sealed record LoadTestOptions
{
    public string HostMode { get; init; } = "LocalTestHost";
    public string Profile { get; init; } = "Smoke";
    public string Scenario { get; init; } = "mixed";
    public string Dataset { get; init; } = "Small";
    public string GeographicDistribution { get; init; } = "Clustered";
    public string BaseUrl { get; init; } = "http://127.0.0.1:5055";
    public string ReportDirectory { get; init; } = "TestResults/loadtests";
    public string AdminEmail { get; init; } = "load-admin@test.local";
    public string AdminPassword { get; init; } = "Load@Test1!";
    public int WarmupSeconds { get; init; } = 15;
    public int TargetDurationSeconds { get; init; } = 30;
    public int IncidentCreateRequestsPerSecond { get; init; } = 2;
    public int IncidentCreateMaxRequests { get; init; } = 500;
    public int CopilotRequestsPerSecond { get; init; } = 1;
    public int QueueJobCount { get; init; } = 1000;
    public int WorkerAIDelayMs { get; init; } = 25;
    public int MediaUploadCount { get; init; } = 20;
    public int MediaImageSizeMb { get; init; } = 10;
    public int MediaVideoSizeMb { get; init; } = 50;
    public int SignalRConnectionCount { get; init; } = 50;
    public int RealAiMaxRequests { get; init; }
    public decimal? RealAiCostEstimateUsd { get; init; }
    public double MaximumErrorRate { get; init; } = 0.01;
    public bool RealProvidersEnabled { get; init; }
    public string? BaselineReportPath { get; init; }
    public double MaximumRelativeLatencyIncrease { get; init; } = 0.20;
    public bool EnforceRegression { get; init; }
    public string? BearerToken { get; init; }
    public string? IncidentId { get; init; }
    public string? ResourceId { get; init; }
    public string? NotificationId { get; init; }

    public TestDataOptions TestData { get; init; } = new();

    public static LoadTestOptions Load(string[] args)
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
        using var document = JsonDocument.Parse(json);
        var options = document.RootElement.GetProperty("LoadTest").Deserialize<LoadTestOptions>(
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Load-test configuration could not be read.");

        var values = ParseArguments(args);
        string EnvOrArg(string key, string env, string fallback) =>
            values.TryGetValue(key, out var value)
                ? value
                : Environment.GetEnvironmentVariable(env) ?? fallback;

        var baseUrl = EnvOrArg("base-url", "BASE_URL", options.BaseUrl);
        var hostMode = EnvOrArg("host-mode", "LOAD_TEST_HOST_MODE", options.HostMode);
        if (Environment.GetEnvironmentVariable("BASE_URL") is not null &&
            !values.ContainsKey("host-mode") &&
            Environment.GetEnvironmentVariable("LOAD_TEST_HOST_MODE") is null)
        {
            hostMode = "External";
        }

        return options with
        {
            HostMode = hostMode,
            Profile = EnvOrArg("profile", "LOAD_TEST_PROFILE", options.Profile),
            Scenario = EnvOrArg("scenario", "LOAD_TEST_SCENARIO", options.Scenario),
            Dataset = EnvOrArg("dataset", "LOAD_TEST_DATASET", options.Dataset),
            GeographicDistribution = EnvOrArg("geography", "LOAD_TEST_GEOGRAPHY", options.GeographicDistribution),
            BaseUrl = baseUrl,
            ReportDirectory = EnvOrArg("report-directory", "LOAD_TEST_REPORT_DIRECTORY", options.ReportDirectory),
            WarmupSeconds = ParseInt(EnvOrArg("warmup-seconds", "LOAD_TEST_WARMUP_SECONDS", options.WarmupSeconds.ToString(CultureInfo.InvariantCulture)), 0, 600, "warmup seconds"),
            TargetDurationSeconds = ParseInt(EnvOrArg("duration-seconds", "LOAD_TEST_DURATION_SECONDS", options.TargetDurationSeconds.ToString(CultureInfo.InvariantCulture)), 1, 86_400, "duration seconds"),
            IncidentCreateRequestsPerSecond = ParseInt(EnvOrArg("incident-create-rps", "INCIDENT_CREATE_RPS", options.IncidentCreateRequestsPerSecond.ToString(CultureInfo.InvariantCulture)), 1, 1000, "incident create RPS"),
            IncidentCreateMaxRequests = ParseInt(EnvOrArg("incident-create-max-requests", "LOAD_TEST_INCIDENT_CREATE_MAX_REQUESTS", "500"), 1, 10_000, "incident create request cap"),
            CopilotRequestsPerSecond = ParseInt(EnvOrArg("copilot-rps", "COPILOT_RPS", options.CopilotRequestsPerSecond.ToString(CultureInfo.InvariantCulture)), 1, 1000, "Copilot RPS"),
            QueueJobCount = ParseInt(EnvOrArg("queue-jobs", "LOAD_TEST_QUEUE_JOBS", "1000"), 1, 10_000, "queued job count"),
            WorkerAIDelayMs = ParseInt(EnvOrArg("worker-ai-delay-ms", "LOAD_TEST_WORKER_AI_DELAY_MS", "25"), 0, 5_000, "worker fake-AI delay in milliseconds"),
            MediaUploadCount = ParseInt(EnvOrArg("media-upload-count", "LOAD_TEST_MEDIA_UPLOAD_COUNT", "20"), 1, 50, "media upload count"),
            MediaImageSizeMb = ParseInt(EnvOrArg("media-image-size-mb", "LOAD_TEST_MEDIA_IMAGE_SIZE_MB", "10"), 1, 10, "image upload size in MiB"),
            MediaVideoSizeMb = ParseInt(EnvOrArg("media-video-size-mb", "LOAD_TEST_MEDIA_VIDEO_SIZE_MB", "50"), 1, 100, "video upload size in MiB"),
            SignalRConnectionCount = ParseInt(EnvOrArg("signalr-connections", "LOAD_TEST_SIGNALR_CONNECTIONS", "50"), 1, 500, "SignalR connection count"),
            RealAiMaxRequests = ParseInt(EnvOrArg("real-ai-max-requests", "LOAD_TEST_REAL_AI_MAX_REQUESTS", "0"), 0, 25, "real AI request cap"),
            RealAiCostEstimateUsd = decimal.TryParse(EnvOrArg("real-ai-cost-estimate-usd", "LOAD_TEST_REAL_AI_COST_ESTIMATE_USD", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out var cost) ? cost : null,
            MaximumErrorRate = ParseDouble(EnvOrArg("max-error-rate", "LOAD_TEST_MAX_ERROR_RATE", options.MaximumErrorRate.ToString(CultureInfo.InvariantCulture)), 0, 1, "maximum error rate"),
            RealProvidersEnabled = bool.TryParse(EnvOrArg("real-ai", "LOAD_TEST_REAL_AI", "false"), out var realProviders) && realProviders,
            BaselineReportPath = EnvOrArg("compare-baseline", "LOAD_TEST_BASELINE_REPORT", ""),
            MaximumRelativeLatencyIncrease = ParseDouble(EnvOrArg("max-latency-regression", "LOAD_TEST_MAX_LATENCY_REGRESSION", "0.20"), 0, 10, "maximum relative latency increase"),
            EnforceRegression = bool.TryParse(EnvOrArg("enforce-regression", "LOAD_TEST_ENFORCE_REGRESSION", "false"), out var enforceRegression) && enforceRegression,
            BearerToken = Environment.GetEnvironmentVariable("BEARER_TOKEN"),
            IncidentId = Environment.GetEnvironmentVariable("LOAD_TEST_INCIDENT_ID"),
            ResourceId = Environment.GetEnvironmentVariable("LOAD_TEST_RESOURCE_ID"),
            NotificationId = Environment.GetEnvironmentVariable("LOAD_TEST_NOTIFICATION_ID")
        };
    }

    private static Dictionary<string, string> ParseArguments(string[] args)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index].StartsWith("--", StringComparison.Ordinal) && index + 1 < args.Length)
            {
                values[args[index][2..]] = args[++index];
            }
        }

        return values;
    }

    private static int ParseInt(string value, int minimum, int maximum, string name)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ||
            parsed < minimum || parsed > maximum)
        {
            throw new InvalidOperationException($"Load-test {name} must be from {minimum} to {maximum}.");
        }

        return parsed;
    }

    private static double ParseDouble(string value, double minimum, double maximum, string name)
    {
        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ||
            parsed < minimum || parsed > maximum)
        {
            throw new InvalidOperationException($"Load-test {name} must be from {minimum} to {maximum}.");
        }

        return parsed;
    }
}

public sealed class TestDataOptions
{
    public int IncidentCount { get; init; } = 100;
    public int HistoryPerIncident { get; init; } = 2;
    public int EscalationCount { get; init; } = 50;
    public int SituationCount { get; init; } = 20;
    public int ResourceAssignmentCount { get; init; } = 50;
    public int NotificationCount { get; init; } = 1000;

    public static TestDataOptions ForProfile(string profile, string dataset)
    {
        if (string.Equals(dataset, "Large", StringComparison.OrdinalIgnoreCase))
        {
            return new TestDataOptions
            {
                IncidentCount = 10_000,
                HistoryPerIncident = 5,
                EscalationCount = 5_000,
                SituationCount = 2_000,
                ResourceAssignmentCount = 10_000,
                NotificationCount = 100_000
            };
        }

        return new TestDataOptions();
    }
}
