using Azure.AI.OpenAI;
using Azure.Identity;

using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AISecurity;
using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.Copilot.Tools;
using EmergencyResponse.Application.AI.DuplicateDetection;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Decision.Interfaces;
using EmergencyResponse.Application.ExternalContext;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Application.Knowledge;
using EmergencyResponse.Application.Incidents;
using EmergencyResponse.Application.Security.FileSecurity;
using EmergencyResponse.Application.Copilot.Actions;
using EmergencyResponse.Application.Predictive;
using EmergencyResponse.Application.Predictive.Escalation;
using EmergencyResponse.Application.Predictive.Forecasting;
using EmergencyResponse.Application.Predictive.Hotspots;
using EmergencyResponse.Application.Predictive.ResourceDemand;
using EmergencyResponse.Application.Predictive.Interfaces;
using EmergencyResponse.Application.Predictive.Training;
using EmergencyResponse.Application.Situations;
using EmergencyResponse.Infrastructure.Decision;
using EmergencyResponse.Infrastructure.Media;
using EmergencyResponse.Infrastructure.Knowledge;
using EmergencyResponse.Infrastructure.Persistence.Repositories;
using EmergencyResponse.Infrastructure.Predictive;
using EmergencyResponse.Infrastructure.Predictive.Forecasting;
using EmergencyResponse.Infrastructure.Predictive.Hotspots;
using EmergencyResponse.Infrastructure.Predictive.ResourceDemand;
using EmergencyResponse.Infrastructure.ExternalContext;
using EmergencyResponse.Infrastructure.Predictive.Training;
using EmergencyResponse.Infrastructure.Situations;
using EmergencyResponse.Infrastructure.Copilot.Tools;
using EmergencyResponse.Infrastructure.Copilot;
using EmergencyResponse.Infrastructure.Copilot.Actions;
using EmergencyResponse.Infrastructure.Security.FileSecurity;

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace EmergencyResponse.Infrastructure.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAI(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.AddSingleton<IAISecurityPolicy, AISecurityPolicy>();
        services.AddSingleton<IPromptShieldClient, DeterministicPromptShieldClient>();
        services.AddScoped<IAIInputSecurityService, AIInputSecurityService>();
        services.AddSingleton<ISensitiveDataDetector, SensitiveDataDetector>();
        services.AddScoped<IAIOutputSecurityService, AIOutputSecurityService>();
        services.AddSingleton<IMalwareScanner, UnavailableMalwareScanner>();
        services.AddScoped<IFileSecurityService, FileSecurityService>();
        services.Configure<KnowledgeDocumentOptions>(
            configuration.GetSection(KnowledgeDocumentOptions.SectionName));
        services.Configure<KnowledgeSearchOptions>(
            configuration.GetSection(KnowledgeSearchOptions.SectionName));
        services.Configure<CopilotMemoryOptions>(
            configuration.GetSection(CopilotMemoryOptions.SectionName));
        services.Configure<CopilotActionOptions>(
            configuration.GetSection(CopilotActionOptions.SectionName));
        var blobServiceUri = configuration["Storage:BlobServiceUri"];
        var mediaContainer = configuration["Storage:IncidentMediaContainer"];
        var knowledgeContainer = configuration["Storage:KnowledgeContainer"];
        if (!string.IsNullOrWhiteSpace(blobServiceUri))
        {
            if (string.IsNullOrWhiteSpace(mediaContainer) ||
                string.IsNullOrWhiteSpace(knowledgeContainer))
            {
                throw new InvalidOperationException(
                    "Storage:IncidentMediaContainer and Storage:KnowledgeContainer are required when Storage:BlobServiceUri is configured.");
            }

            var credential = new DefaultAzureCredential();
            services.AddSingleton<IIncidentMediaStorage>(_ =>
                new AzureBlobIncidentMediaStorage(
                    new Uri(blobServiceUri), mediaContainer, credential));
            services.AddSingleton<IKnowledgeDocumentStorage>(_ =>
                new AzureBlobKnowledgeDocumentStorage(
                    new Uri(blobServiceUri), knowledgeContainer, credential));
        }
        else
        {
            services.AddSingleton<IKnowledgeDocumentStorage,
                LocalKnowledgeDocumentStorage>();
            services.AddSingleton<IIncidentMediaStorage,
                LocalIncidentMediaStorage>();
        }
        services.AddScoped<IKnowledgeDocumentNumberGenerator,
            KnowledgeDocumentNumberGenerator>();
        services.AddSingleton<IKnowledgeDocumentParser,
            KnowledgeDocumentParser>();
        services.AddSingleton<IKnowledgeDocumentChunker,
            KnowledgeDocumentChunker>();
        services.AddSingleton<KnowledgeSearchIndexBuilder>();
        services.AddScoped<IKnowledgeSearchService,
            KnowledgeSearchService>();
        services.AddScoped<IKnowledgeAccessContextFactory,
            KnowledgeAccessContextFactory>();
        services.AddScoped<IKnowledgeDocumentService,
            KnowledgeDocumentService>();
        services.AddSingleton<IKnowledgeDocumentQueue,
            InMemoryKnowledgeDocumentQueue>();
        services.AddHostedService<KnowledgeDocumentBackgroundWorker>();
        services.AddScoped<IEmergencyCopilotService,
            EmergencyCopilotService>();
        services.AddScoped<ICitationValidator, CitationValidator>();
        services.AddScoped<ICopilotPolicyValidator, CopilotPolicyValidator>();
        services.AddScoped<IConversationMemoryService, ConversationMemoryService>();
        services.AddScoped<ICopilotFeedbackService, CopilotFeedbackService>();
        services.AddScoped<ICopilotActionPolicy, CopilotActionPolicy>();
        services.AddScoped<ICopilotActionService, CopilotActionService>();
        services.AddScoped<ICopilotActionExecutor, CopilotActionExecutor>();
        services.AddScoped<CopilotExecutionContext>();
        services.AddScoped<
            EmergencyResponse.Application.Copilot.Tools.ICopilotExecutionContext>(
            sp => sp.GetRequiredService<CopilotExecutionContext>());
        services.AddScoped<CopilotToolAuthorization>();
        services.AddScoped<CopilotToolInvocationRecorder>();
        services.AddScoped<IncidentCopilotTools>();
        services.AddScoped<SituationCopilotTools>();
        services.AddScoped<ResourceCopilotTools>();
        services.AddScoped<EscalationCopilotTools>();
        services.AddScoped<EnvironmentCopilotTools>();
        services.AddScoped<NotificationCopilotTools>();
        services.AddScoped<PredictionCopilotTools>();
        services.AddScoped<KnowledgeCopilotTools>();
        services.AddScoped<ActionCopilotTools>();
        services.Configure<AzureMapsOptions>(
            configuration.GetSection(AzureMapsOptions.SectionName));
        services.AddHttpClient<IWeatherContextClient, AzureMapsWeatherClient>();
        services.AddSingleton<IExternalContextQueue, InMemoryExternalContextQueue>();
        services.AddScoped<
            IIncidentExternalContextService,
            IncidentExternalContextService>();
        services.AddHostedService<ExternalContextBackgroundWorker>();

        var options =
            configuration
                .GetSection(AIOptions.SectionName)
                .Get<AIOptions>()
            ?? throw new InvalidOperationException(
                "AI configuration is missing.");

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            throw new InvalidOperationException(
                "AI endpoint is not configured.");
        }

        if (string.IsNullOrWhiteSpace(options.Deployment))
        {
            throw new InvalidOperationException(
                "AI deployment is not configured.");
        }
        var azureClient = !string.IsNullOrWhiteSpace(options.ApiKey)
            ? new AzureOpenAIClient(
                new Uri(options.Endpoint),
                new System.ClientModel.ApiKeyCredential(options.ApiKey))
            : new AzureOpenAIClient(
                new Uri(options.Endpoint),
                new DefaultAzureCredential());

        services.AddSingleton(azureClient);

        var chatClient =
            azureClient
                .GetChatClient(options.Deployment)
                .AsIChatClient();

        var embeddingGenerator =
            azureClient
                .GetEmbeddingClient(options.EmbeddingDeployment)
                .AsIEmbeddingGenerator();

        services.AddChatClient(chatClient);

        services.AddScoped<
            IIncidentAnalysisService,
            IncidentAnalysisService>();

        services.AddScoped<
            IAIAnalysisRepository,
            AIAnalysisRepository>();

        services.AddScoped<
            IIncidentAIOrchestrationService,
            IncidentAIOrchestrationService>();

        services.AddSingleton<
            ISeverityEngine,
            SeverityEngine>();

        services.AddSingleton<
            IPriorityEngine,
            PriorityEngine>();

        services.AddScoped<
            IIncidentDecisionService,
            IncidentDecisionService>();

        services.AddSingleton<IAIAnalysisQueue,
            InMemoryAIAnalysisQueue>();

        services.AddScoped<
            IIncidentAIJobProcessor,
            IncidentAIJobProcessor>();

        services.AddHostedService<
            IncidentAIBackgroundWorker>();

        services.AddSingleton<
            IAIReviewPolicy,
            AIReviewPolicy>();

        services.AddSingleton<
            IEmbeddingGenerator<string, Embedding<float>>
        >(embeddingGenerator);

        services.AddScoped<IIncidentEmbeddingService, IncidentEmbeddingService>();

        services.AddScoped<
            IDuplicateIncidentService,
            IncidentDuplicateService>();

        services.AddScoped<
            IIncidentConsolidationService,
            IncidentConsolidationService>();

        services.AddHttpClient<
            IIncidentVisualAnalysisService,
            IncidentVisualAnalysisService>();

        services.AddScoped<
            IVisualReviewPolicy,
            VisualReviewPolicy>();

        services.AddSingleton<
            IVisualAnalysisQueue,
            InMemoryVisualAnalysisQueue>();

        services.AddScoped<
            VisualAnalysisOrchestrationService>();

        services.AddHostedService<
            VisualAnalysisBackgroundWorker>();

        services.AddScoped<
            IEscalationPredictionFeatureBuilder,
            EscalationPredictionFeatureBuilder>();

        services.AddSingleton<
            IPredictiveAnalysisQueue,
            InMemoryPredictiveAnalysisQueue>();

        services.AddScoped<
            IEscalationRiskPredictionService,
            EscalationRiskPredictionService>();

        services.AddScoped<
            IIncidentPredictionSnapshotBuilder,
            IncidentPredictionSnapshotBuilder>();

        services.AddScoped<
            IEscalationLabelGenerator,
            EscalationLabelGenerator>();

        services.AddHostedService<
            PredictiveAnalysisBackgroundWorker>();

        services.AddHostedService<
            ForecastGenerationBackgroundWorker>();

        services.AddScoped<
            IForecastingService,
            IncidentVolumeForecastService>();

        services.AddScoped<
            IResourceDemandForecastService,
            ResourceDemandForecastService>();

        services.AddScoped<
            IHotspotService,
            HotspotService>();

        services.Configure<HotspotOptions>(
            configuration.GetSection("Predictive:Hotspots"));

        services.AddScoped<
            IIncidentMediaService,
            IncidentMediaService>();

        services.Configure<VideoFrameExtractionOptions>(
            configuration.GetSection("VideoFrameExtraction"));

        services.AddScoped<
            IVideoFrameExtractor,
            FfmpegVideoFrameExtractor>();

        services.AddScoped<
            ISituationAnalysisService,
            SituationAnalysisService>();

        return services;
    }
}


public static class DependencyInjectionTest
{
    /// <summary>
    /// Registers the minimum AI infrastructure required for integration tests
    /// without touching Azure credentials or real AI endpoints.
    /// Real AI service interfaces are replaced by the test factory with no-op stubs.
    /// </summary>
    public static IServiceCollection AddTestAI(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IAISecurityPolicy, AISecurityPolicy>();
        services.AddSingleton<IPromptShieldClient, DeterministicPromptShieldClient>();
        services.AddScoped<IAIInputSecurityService, AIInputSecurityService>();
        services.AddSingleton<ISensitiveDataDetector, SensitiveDataDetector>();
        services.AddScoped<IAIOutputSecurityService, AIOutputSecurityService>();
        services.Configure<AIOptions>(
            configuration.GetSection(AIOptions.SectionName));

        services.AddSingleton<ISeverityEngine, SeverityEngine>();
        services.AddSingleton<IPriorityEngine, PriorityEngine>();
        services.AddScoped<IIncidentDecisionService, IncidentDecisionService>();

        services.AddSingleton<IAIAnalysisQueue, InMemoryAIAnalysisQueue>();
        services.AddScoped<IIncidentAIJobProcessor, IncidentAIJobProcessor>();
        services.AddHostedService<IncidentAIBackgroundWorker>();
        services.AddSingleton<IAIReviewPolicy, AIReviewPolicy>();

        services.AddScoped<IAIAnalysisRepository, AIAnalysisRepository>();
        services.AddScoped<IIncidentAIOrchestrationService, IncidentAIOrchestrationService>();

        services.AddScoped<IIncidentConsolidationService, IncidentConsolidationService>();

        services.AddSingleton<IIncidentMediaStorage, LocalIncidentMediaStorage>();
        services.AddSingleton<IVisualAnalysisQueue, InMemoryVisualAnalysisQueue>();
        services.AddScoped<VisualAnalysisOrchestrationService>();
        services.AddHostedService<VisualAnalysisBackgroundWorker>();
        services.AddScoped<IIncidentMediaService, IncidentMediaService>();

        services.Configure<VideoFrameExtractionOptions>(
            configuration.GetSection("VideoFrameExtraction"));

        // IIncidentAnalysisService, IIncidentVisualAnalysisService,
        // IVideoFrameExtractor, IIncidentEmbeddingService, IDuplicateIncidentService
        // are injected as no-ops by the test WebApplicationFactory.
        // Register null-safe stubs here so DI doesn't fail before the factory
        // gets a chance to replace them.
        services.AddScoped<IIncidentAnalysisService, NullIncidentAnalysisService>();
        services.AddScoped<IIncidentVisualAnalysisService, NullIncidentVisualAnalysisService>();
        services.AddScoped<IVideoFrameExtractor, NullVideoFrameExtractor>();
        services.AddScoped<IIncidentEmbeddingService, NullIncidentEmbeddingService>();
        services.AddScoped<IDuplicateIncidentService, NullDuplicateIncidentService>();
        services.AddScoped<ISituationAnalysisService, NullSituationAnalysisService>();

        return services;
    }

    // ── Null-object stubs for test environment ────────────────────────────

    private sealed class NullSituationAnalysisService : ISituationAnalysisService
    {
        public Task<EmergencyResponse.Application.Situations.SituationAnalysisResult> AnalyzeSituationAsync(
            EmergencyResponse.Application.Situations.SituationSnapshot snapshot,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmergencyResponse.Application.Situations.SituationAnalysisResult
            {
                SituationTitle = "Test Operational Situation",
                SituationSummary = "Operational test summary.",
                KeyObservations = ["Test Cluster"],
                RecommendedActions = ["Test Coordination"],
                ConfidenceExplanation = "Stable",
                Confidence = 0.9
            });

        public Task<EmergencyResponse.Application.Situations.SituationAnalysisResult> AnalyzeAsync(
            string situationContext,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmergencyResponse.Application.Situations.SituationAnalysisResult
            {
                SituationTitle = "Test Operational Situation",
                SituationSummary = "Operational test summary.",
                KeyObservations = ["Test Cluster"],
                RecommendedActions = ["Test Coordination"],
                ConfidenceExplanation = "Stable",
                Confidence = 0.9
            });
    }

    private sealed class NullIncidentAnalysisService : IIncidentAnalysisService
    {
        public Task<EmergencyResponse.Application.AI.DTOs.IncidentAnalysisResult> AnalyzeAsync(
            EmergencyResponse.Application.AI.DTOs.IncidentAnalysisInput input,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmergencyResponse.Application.AI.DTOs.IncidentAnalysisResult
            {
                IncidentType      = input.IncidentType ?? "Unknown",
                SeverityCandidate = "Low",
                Facts             = new EmergencyResponse.Application.AI.DTOs.EmergencyFacts(),
                Confidence        = 0.5
            });
    }

    private sealed class NullIncidentVisualAnalysisService : IIncidentVisualAnalysisService
    {
        public Task<EmergencyResponse.Application.AI.VisualAnalysis.VisualAnalysisResult> AnalyzeImageAsync(
            Stream imageStream, string contentType, string? incidentContext,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmergencyResponse.Application.AI.VisualAnalysis.VisualAnalysisResult
            {
                SceneDescription = "Test",
                Confidence       = 0.5
            });

        public Task<EmergencyResponse.Application.AI.VisualAnalysis.VisualAnalysisResult> AnalyzeVideoFramesAsync(
            IReadOnlyList<EmergencyResponse.Application.Media.VideoFrame> frames,
            string? incidentContext,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new EmergencyResponse.Application.AI.VisualAnalysis.VisualAnalysisResult
            {
                SceneDescription = "Test",
                Confidence       = 0.5
            });
    }

    private sealed class NullVideoFrameExtractor : IVideoFrameExtractor
    {
        public Task<IReadOnlyList<EmergencyResponse.Application.Media.VideoFrame>>
            ExtractRepresentativeFramesAsync(
                Stream videoStream,
                CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmergencyResponse.Application.Media.VideoFrame>>(
                Array.Empty<EmergencyResponse.Application.Media.VideoFrame>());
    }

    private sealed class NullIncidentEmbeddingService : IIncidentEmbeddingService
    {
        public Task<ReadOnlyMemory<float>> GenerateAsync(
            string text,
            CancellationToken cancellationToken = default)
            => Task.FromResult(new ReadOnlyMemory<float>(new float[1536]));
    }

    private sealed class NullDuplicateIncidentService : IDuplicateIncidentService
    {
        public Task<IReadOnlyList<EmergencyResponse.Application.AI.DTOs.DuplicateIncidentCandidate>>
            FindCandidatesAsync(
                Guid incidentId,
                int maxCandidates = 20,
                CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<EmergencyResponse.Application.AI.DTOs.DuplicateIncidentCandidate>>(
                Array.Empty<EmergencyResponse.Application.AI.DTOs.DuplicateIncidentCandidate>());

        public Task GenerateEmbeddingAsync(
            Guid incidentId,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
