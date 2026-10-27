using System.Runtime.CompilerServices;
using System.Text.Json;
using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.AI.VisualAnalysis;
using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.ExternalContext;
using EmergencyResponse.Application.Knowledge;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Application.Situations;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.LoadTests.Infrastructure;

public sealed class LoadTestIncidentAnalysisService(LoadTestFaultController faults) : IIncidentAnalysisService
{
    public async Task<IncidentAnalysisResult> AnalyzeAsync(IncidentAnalysisInput input, CancellationToken cancellationToken = default)
    {
        await faults.ApplyAIAsync(cancellationToken);
        return new IncidentAnalysisResult
        {
            IncidentType = input.IncidentType ?? "Other",
            SeverityCandidate = "Low",
            RecommendedDepartment = "Disaster Response",
            Facts = new EmergencyFacts(),
            Confidence = 0.75
        };
    }
}

public sealed class LoadTestVisualAnalysisService(LoadTestFaultController faults) : IIncidentVisualAnalysisService
{
    public async Task<VisualAnalysisResult> AnalyzeImageAsync(Stream imageStream, string contentType, string? incidentContext, CancellationToken cancellationToken = default)
    {
        await faults.ApplyAIAsync(cancellationToken);
        return new VisualAnalysisResult { SceneDescription = "Synthetic load-test scene.", Confidence = 0.8 };
    }

    public async Task<VisualAnalysisResult> AnalyzeVideoFramesAsync(IReadOnlyList<VideoFrame> frames, string? incidentContext, CancellationToken cancellationToken = default)
    {
        await faults.ApplyAIAsync(cancellationToken);
        return new VisualAnalysisResult { SceneDescription = "Synthetic load-test video.", Confidence = 0.8 };
    }
}

public sealed class LoadTestVideoFrameExtractor : IVideoFrameExtractor
{
    public Task<IReadOnlyList<VideoFrame>> ExtractRepresentativeFramesAsync(Stream videoStream, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<VideoFrame>>([]);
}

public sealed class LoadTestSituationAnalysisService(LoadTestFaultController faults) : ISituationAnalysisService
{
    public async Task<SituationAnalysisResult> AnalyzeSituationAsync(SituationSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        await faults.ApplyAIAsync(cancellationToken);
        return Result();
    }

    public async Task<SituationAnalysisResult> AnalyzeAsync(string situationContext, CancellationToken cancellationToken = default)
    {
        await faults.ApplyAIAsync(cancellationToken);
        return Result();
    }

    private static SituationAnalysisResult Result() => new()
    {
        SituationTitle = "Synthetic load-test situation",
        SituationSummary = "Synthetic operational context.",
        KeyObservations = ["Load-test fixture"],
        MainHazards = ["None asserted"],
        OperationalConcerns = [],
        RecommendedActions = ["Review current operational data."],
        ConfidenceExplanation = "Deterministic test response.",
        Confidence = 0.8
    };
}

public sealed class LoadTestEmbeddingService(LoadTestFaultController faults) : IIncidentEmbeddingService
{
    public async Task<ReadOnlyMemory<float>> GenerateAsync(string text, CancellationToken cancellationToken = default)
    {
        await faults.ApplyAIAsync(cancellationToken);
        return new ReadOnlyMemory<float>(new float[1536]);
    }
}

public sealed class LoadTestEmbeddingGenerator(LoadTestFaultController faults) : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata => new("load-test", null, "deterministic");

    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
    {
        await faults.ApplyAIAsync(cancellationToken);
        return new GeneratedEmbeddings<Embedding<float>>(values.Select(_ => new Embedding<float>(new float[1536])).ToList());
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

public sealed class LoadTestKnowledgeSearchService(LoadTestFaultController faults) : IKnowledgeSearchService
{
    public bool IsAvailable => !faults.SearchUnavailable;

    public Task<IReadOnlyList<KnowledgeSearchResult>> SearchAsync(string question, KnowledgeAccessContext accessContext, CancellationToken cancellationToken = default)
    {
        if (faults.SearchUnavailable)
        {
            throw new HttpRequestException("Injected load-test search outage.");
        }

        IReadOnlyList<KnowledgeSearchResult> results =
        [
            new KnowledgeSearchResult
            {
                SearchDocumentId = "LOADTEST-DOC-1-v1-p1-1",
                DocumentNumber = "LOADTEST-DOC-1",
                Title = "Synthetic emergency procedure",
                Version = "1",
                PageNumber = 1,
                SectionHeading = "Load test",
                Content = "This synthetic result exists only to measure authorized retrieval overhead.",
                Score = 0.9,
                DocumentType = "Procedure"
            }
        ];
        return Task.FromResult(results);
    }

    public Task IndexAsync(KnowledgeDocumentIndexRequest request, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task DeleteAsync(IReadOnlyList<string> searchDocumentIds, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

public sealed class LoadTestWeatherContextClient(LoadTestFaultController faults) : IWeatherContextClient
{
    public Task<WeatherContextData?> GetCurrentAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        if (faults.WeatherUnavailable) throw new HttpRequestException("Injected load-test weather outage.");
        return Task.FromResult<WeatherContextData?>(new WeatherContextData { ObservedAtUtc = DateTime.UtcNow, WeatherPhrase = "Synthetic test context" });
    }

    public Task<IReadOnlyList<WeatherForecastPoint>> GetHourlyForecastAsync(double latitude, double longitude, int durationHours, CancellationToken cancellationToken = default)
    {
        if (faults.WeatherUnavailable) throw new HttpRequestException("Injected load-test weather outage.");
        return Task.FromResult<IReadOnlyList<WeatherForecastPoint>>([]);
    }

    public Task<IReadOnlyList<SevereWeatherAlert>> GetSevereAlertsAsync(double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        if (faults.WeatherUnavailable) throw new HttpRequestException("Injected load-test weather outage.");
        return Task.FromResult<IReadOnlyList<SevereWeatherAlert>>([]);
    }
}

public sealed class LoadTestMediaStorage(LoadTestFaultController faults) : IIncidentMediaStorage, IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "EmergencyResponseLoadTests", Guid.NewGuid().ToString("N"));
    private int _disposed;

    public string TemporaryDirectory => _root;
    public int TemporaryFileCount => Directory.Exists(_root) ? Directory.GetFiles(_root).Length : 0;

    public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        var key = $"{Guid.NewGuid():N}{extension}";
        Directory.CreateDirectory(_root);
        await using var output = new FileStream(Resolve(key), FileMode.CreateNew, FileAccess.Write, FileShare.Read,
            64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(output, cancellationToken);
        return key;
    }

    public async Task SaveWithKeyAsync(string key, Stream content, CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        Directory.CreateDirectory(_root);
        await using var output = new FileStream(Resolve(key), FileMode.Create, FileAccess.Write, FileShare.Read,
            64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await content.CopyToAsync(output, cancellationToken);
    }

    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        var path = Resolve(storageKey);
        return Task.FromResult<Stream>(File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan)
            : throw new FileNotFoundException("Synthetic load-test media was not found."));
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        var path = Resolve(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    public void Cleanup()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        var tempRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "EmergencyResponseLoadTests"));
        var fullRoot = Path.GetFullPath(_root);
        if (!fullRoot.StartsWith(tempRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Refusing to clean a media test directory outside the dedicated temporary root.");
        }

        if (Directory.Exists(fullRoot)) Directory.Delete(fullRoot, recursive: true);
    }

    public void Dispose() => Cleanup();

    private string Resolve(string key)
    {
        var safeKey = Path.GetFileName(key);
        if (string.IsNullOrWhiteSpace(safeKey)) throw new InvalidOperationException("Invalid synthetic media key.");
        return Path.Combine(_root, safeKey);
    }

    private void EnsureAvailable()
    {
        if (faults.StorageUnavailable) throw new IOException("Injected load-test storage outage.");
    }
}

public sealed class LoadTestChatClient(LoadTestFaultController faults) : IChatClient
{
    public async Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        await faults.ApplyAIAsync(cancellationToken);
        var prompt = string.Join("\n", messages.Select(message => message.Text));
        object response = prompt.Contains("emergency incident analysis assistant", StringComparison.OrdinalIgnoreCase)
            ? new IncidentAnalysisResult { IncidentType = "Other", SeverityCandidate = "Low", Facts = new EmergencyFacts(), Confidence = 0.75 }
            : prompt.Contains("situation", StringComparison.OrdinalIgnoreCase)
                ? new SituationAnalysisResult { SituationTitle = "Synthetic situation", SituationSummary = "Synthetic situation analysis.", KeyObservations = [], MainHazards = [], OperationalConcerns = [], RecommendedActions = [], ConfidenceExplanation = "Synthetic", Confidence = 0.75 }
                : new EmergencyCopilotStructuredResponse { Answer = "Synthetic load-test response.", SourcesSufficient = false, RequiresHumanVerification = true, SourcesInsufficiencyReason = "Synthetic data only." };

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, JsonSerializer.Serialize(response)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;
    public void Dispose() { }
}

public sealed class LoadTestApplicationMessageBus(
    IServiceScopeFactory scopes,
    LoadTestFaultController faults,
    IOptions<AIOptions> aiOptions) : EmergencyResponse.Application.Reliability.IApplicationMessageBus
{
    public Task PublishAsync<T>(string messageType, T message, string messageId, CancellationToken cancellationToken = default)
    {
        if (faults.PublishUnavailable) throw new HttpRequestException("Injected load-test message-bus outage.");
        return new EmergencyResponse.Infrastructure.BackgroundJobs.InMemoryApplicationMessageBus(scopes, aiOptions)
            .PublishAsync(messageType, message, messageId, cancellationToken);
    }
}
