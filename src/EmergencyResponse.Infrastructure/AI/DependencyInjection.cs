using Azure.AI.OpenAI;
using Azure.Identity;

using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.DuplicateDetection;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Decision.Interfaces;
using EmergencyResponse.Application.Media;
using EmergencyResponse.Infrastructure.Decision;
using EmergencyResponse.Infrastructure.Media;
using EmergencyResponse.Infrastructure.Persistence.Repositories;

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

        var credential = new AzureCliCredential();

        var azureClient = new AzureOpenAIClient(
            new Uri(options.Endpoint),
            credential);

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

        services.AddSingleton<IIncidentMediaStorage,
            LocalIncidentMediaStorage>();

        services.AddScoped<
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
            IIncidentMediaService,
            IncidentMediaService>();

        services.Configure<VideoFrameExtractionOptions>(
            configuration.GetSection("VideoFrameExtraction"));

        services.AddScoped<
            IVideoFrameExtractor,
            FfmpegVideoFrameExtractor>();

        return services;
    }
}