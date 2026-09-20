using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.VisualAnalysis;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Infrastructure.Persistence;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EmergencyResponse.IntegrationTests;

/// <summary>
/// WebApplicationFactory that runs the API against a SQLite temp-file database
/// (the "Test" environment branch in Program.cs) with AI services stubbed.
/// Each factory instance gets its own unique DB file for test isolation.
/// </summary>
public sealed class ApiWebApplicationFactory
    : WebApplicationFactory<Program>
{
    // Each factory instance gets a unique SQLite file so tests are isolated.
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(),
            $"er-integration-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // "Test" environment causes Program.cs to use SQLite instead of SQL Server.
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
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

                // SQLite temp-file database — persists across connections.
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath}"
            });
        });

        builder.ConfigureServices(services =>
        {
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

            // ── Replace local media storage with temp-dir ──────────────────
            services.RemoveAll<IIncidentMediaStorage>();
            services.AddSingleton<IIncidentMediaStorage>(
                new TempDirMediaStorage());
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch { /* best-effort */ }
        }
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
