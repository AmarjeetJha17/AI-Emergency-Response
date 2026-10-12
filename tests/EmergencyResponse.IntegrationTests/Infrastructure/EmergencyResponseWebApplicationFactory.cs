using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.VisualAnalysis;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Application.Situations;
using EmergencyResponse.Infrastructure.Persistence;
using EmergencyResponse.IntegrationTests.Infrastructure;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace EmergencyResponse.IntegrationTests;

/// <summary>
/// WebApplicationFactory using the dedicated SQL Server test database and deterministic AI fakes.
/// </summary>
public sealed class EmergencyResponseWebApplicationFactory
    : WebApplicationFactory<Program>
{
    public ManualTimeProvider TestTimeProvider { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // CI and local test processes should not need permission to write to
        // the machine-wide Windows Event Log.
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.Test.json"),
                optional: false,
                reloadOnChange: false);
            config.AddEnvironmentVariables();

            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Test:RelaxRateLimits"] = "true",
                ["Jwt:Issuer"]            = "IntegrationTests",
                ["Jwt:Audience"]          = "IntegrationTests",
                ["Jwt:SecretKey"]         = "integration-test-secret-key-minimum-32-chars!",
                ["Jwt:ExpirationMinutes"] = "60",

                ["AdminSeed:Email"]     = "admin@test.local",
                ["AdminSeed:Password"]  = "Admin@Test1!",
                ["AdminSeed:FirstName"] = "Test",
                ["AdminSeed:LastName"]  = "Admin",

                ["AI:Provider"]            = "AzureOpenAI",
                ["AI:Endpoint"]            = "https://test.openai.azure.com/",
                ["AI:Deployment"]          = "test-deployment",
                ["AI:Model"]               = "test-model",
                ["AI:EmbeddingDeployment"] = "test-embedding",

                ["IncidentMediaStorage:RootPath"] =
                    Path.Combine(Path.GetTempPath(), "ErIntegrationTests"),

                ["Security:RateLimits:AuthenticationPerMinute"] = "1000",
                ["Security:RateLimits:IncidentCreatePerMinute"] = "1000",
                ["Security:RateLimits:MediaUploadPerMinute"] = "1000",
                ["Security:RateLimits:CopilotPerMinute"] = "1000",
                ["Security:RateLimits:AdminPerMinute"] = "1000",

            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(TestTimeProvider);

            // ── Stub AI services at the Application layer ─────────────────
            // These replace the null-object stubs wired by AddTestAI().
            services.RemoveAll<IIncidentAnalysisService>();
            services.AddScoped<IIncidentAnalysisService,
                NoOpIncidentAnalysisService>();

            services.RemoveAll<IIncidentVisualAnalysisService>();
            services.AddScoped<IIncidentVisualAnalysisService,
                NoOpVisualAnalysisService>();

            services.RemoveAll<IVideoFrameExtractor>();
            services.AddScoped<IVideoFrameExtractor, NoOpVideoFrameExtractor>();

            services.RemoveAll<IEmbeddingGenerator<string, Embedding<float>>>();
            services.AddSingleton<
                IEmbeddingGenerator<string, Embedding<float>>,
                NoOpEmbeddingGenerator>();

            services.RemoveAll<ISituationAnalysisService>();
            services.AddScoped<ISituationAnalysisService,
                NoOpSituationAnalysisService>();

            // ── Replace local media storage with temp-dir ──────────────────
            services.RemoveAll<IIncidentMediaStorage>();
            services.AddSingleton<IIncidentMediaStorage>(
                new TempDirMediaStorage());

            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = IntegrationTestScheme.Selector;
                    options.DefaultAuthenticateScheme = IntegrationTestScheme.Selector;
                    options.DefaultChallengeScheme = IntegrationTestScheme.Selector;
                })
                .AddPolicyScheme(IntegrationTestScheme.Selector, null, options =>
                {
                    options.ForwardDefaultSelector = context =>
                        context.Request.Headers.ContainsKey("X-Test-UserId")
                            ? TestAuthenticationHandler.Scheme
                            : JwtBearerDefaults.AuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.Scheme,
                    _ => { });
        });
    }

    private static class IntegrationTestScheme
    {
        public const string Selector = "IntegrationTestSelector";
    }

    // ── No-op stubs ───────────────────────────────────────────────────────

    private sealed class NoOpIncidentAnalysisService : IIncidentAnalysisService
    {
        public Task<IncidentAnalysisResult> AnalyzeAsync(
            IncidentAnalysisInput input,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new IncidentAnalysisResult
            {
                IncidentType          = input.IncidentType ?? "Fire",
                SeverityCandidate     = "Low",
                Facts                 = new EmergencyFacts(),
                RecommendedDepartment = "Fire",
                Confidence            = 0.5
            });
    }

    private sealed class NoOpSituationAnalysisService : ISituationAnalysisService
    {
        public Task<SituationAnalysisResult> AnalyzeSituationAsync(
            SituationSnapshot snapshot,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new SituationAnalysisResult
            {
                SituationTitle = "Test Operational Situation",
                SituationSummary = "Automated test situation summary.",
                KeyObservations = ["Observation 1", "Observation 2"],
                MainHazards = ["Hazard 1"],
                OperationalConcerns = ["Concern 1"],
                RecommendedActions = ["Action 1"],
                ConfidenceExplanation = "High Confidence",
                Confidence = 0.95
            });

        public Task<SituationAnalysisResult> AnalyzeAsync(
            string situationContext,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new SituationAnalysisResult
            {
                SituationTitle = "Test Operational Situation",
                SituationSummary = "Automated test situation summary.",
                KeyObservations = ["Observation 1", "Observation 2"],
                MainHazards = ["Hazard 1"],
                OperationalConcerns = ["Concern 1"],
                RecommendedActions = ["Action 1"],
                ConfidenceExplanation = "High Confidence",
                Confidence = 0.95
            });
    }

    private sealed class NoOpVisualAnalysisService
        : IIncidentVisualAnalysisService
    {
        public Task<VisualAnalysisResult> AnalyzeImageAsync(
            Stream imageStream, string contentType,
            string? incidentContext,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VisualAnalysisResult
            {
                SceneDescription = "Test scene",
                Confidence = 0.5
            });

        public Task<VisualAnalysisResult> AnalyzeVideoFramesAsync(
            IReadOnlyList<VideoFrame> frames,
            string? incidentContext,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new VisualAnalysisResult
            {
                SceneDescription = "Test scene",
                Confidence = 0.5
            });
    }

    private sealed class NoOpVideoFrameExtractor : IVideoFrameExtractor
    {
        public Task<IReadOnlyList<VideoFrame>> ExtractRepresentativeFramesAsync(
            Stream videoStream,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<VideoFrame>>(
                Array.Empty<VideoFrame>());
    }

    private sealed class NoOpEmbeddingGenerator
        : IEmbeddingGenerator<string, Embedding<float>>
    {
        public EmbeddingGeneratorMetadata Metadata =>
            new("no-op", null, "no-op");

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values,
            EmbeddingGenerationOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var embeddings = values
                .Select(_ => new Embedding<float>(new float[1536]))
                .ToList();
            return Task.FromResult(
                new GeneratedEmbeddings<Embedding<float>>(embeddings));
        }

        public object? GetService(Type serviceType, object? serviceKey = null)
            => null;

        public void Dispose() { }
    }

    private sealed class TempDirMediaStorage : IIncidentMediaStorage
    {
        private readonly string _root =
            Path.Combine(Path.GetTempPath(), "ErIntegrationTests");

        public async Task<string> SaveAsync(
            Stream content, string extension,
            CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(_root);
            var key  = $"{Guid.NewGuid():N}{extension}";
            var path = Path.Combine(_root, key);
            await using var fs = File.Create(path);
            await content.CopyToAsync(fs, cancellationToken);
            return key;
        }

        public async Task SaveWithKeyAsync(
            string key, Stream content,
            CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(
                _root, key.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var fs = File.Create(path);
            await content.CopyToAsync(fs, cancellationToken);
        }

        public Task<Stream> OpenReadAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(
                _root, key.Replace('/', Path.DirectorySeparatorChar));
            return Task.FromResult<Stream>(File.OpenRead(path));
        }

        public Task DeleteAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            var path = Path.Combine(
                _root, key.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path)) File.Delete(path);
            return Task.CompletedTask;
        }
    }
}
