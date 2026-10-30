using Azure.AI.OpenAI;
using EmergencyResponse.Application.AI;
using EmergencyResponse.Application.AI.DuplicateDetection;
using EmergencyResponse.Application.AI.Interfaces;
using EmergencyResponse.Application.Common.Configuration;
using EmergencyResponse.Application.Copilot;
using EmergencyResponse.Application.Exceptions;
using EmergencyResponse.Application.Situations;
using EmergencyResponse.Api.Common.ErrorHandling;
using EmergencyResponse.Infrastructure.AI;
using EmergencyResponse.Infrastructure.Predictive;
using EmergencyResponse.Infrastructure.BackgroundJobs;
using EmergencyResponse.Infrastructure.Knowledge;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EmergencyResponse.UnitTests.Configuration;

public sealed class AIRegistrationTests
{
    [Fact]
    public void ForecastWorkerRegistration_IsOptInForPortfolio_AndPreservesOtherWorkers()
    {
        foreach (var enabled in new[] { false, true })
        {
            var configuration = CreateConfiguration();
            configuration["AI:Enabled"] = "false";
            configuration["Forecasting:BackgroundWorkerEnabled"] = enabled.ToString();
            var services = CreateServices(configuration);
            services.AddAI(configuration);

            var hostedTypes = services
                .Where(d => d.ServiceType == typeof(IHostedService))
                .Select(d => d.ImplementationType)
                .ToArray();

            Assert.Equal(enabled, hostedTypes.Contains(typeof(ForecastGenerationBackgroundWorker)));
            Assert.DoesNotContain(typeof(IncidentAIBackgroundWorker), hostedTypes);
            Assert.DoesNotContain(typeof(VisualAnalysisBackgroundWorker), hostedTypes);
            Assert.DoesNotContain(typeof(KnowledgeDocumentBackgroundWorker), hostedTypes);
            Assert.Contains(typeof(PredictiveAnalysisBackgroundWorker), hostedTypes);
        }
    }

    [Fact]
    public void EnabledAI_RegistersAIQueueWorkers()
    {
        var configuration = CreateConfiguration();
        configuration["AI:Enabled"] = "true";
        var services = CreateServices(configuration);
        services.AddAI(configuration);

        var hostedTypes = services
            .Where(d => d.ServiceType == typeof(IHostedService))
            .Select(d => d.ImplementationType)
            .ToArray();

        Assert.Contains(typeof(IncidentAIBackgroundWorker), hostedTypes);
        Assert.Contains(typeof(VisualAnalysisBackgroundWorker), hostedTypes);
        Assert.Contains(typeof(KnowledgeDocumentBackgroundWorker), hostedTypes);
        Assert.Contains(typeof(PredictiveAnalysisBackgroundWorker), hostedTypes);
    }

    [Fact]
    public void DataRetentionWorkerRegistration_RespectsExplicitPortfolioOptOutAndPreservesDefault()
    {
        foreach (var enabled in new bool?[] { false, true, null })
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(
                enabled is null
                    ? new Dictionary<string, string?>()
                    : new Dictionary<string, string?>
                    {
                        ["DataRetention:BackgroundWorkerEnabled"] = enabled.Value.ToString()
                    }).Build();
            var services = new ServiceCollection();
            services.AddDataRetentionBackgroundWorker(configuration);

            var registered = services.Any(d => d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(DataRetentionBackgroundWorker));
            Assert.Equal(enabled ?? true, registered);
        }
    }

    [Fact]
    public void ForecastWorkerRegistration_DefaultsToExistingBehavior()
    {
        var configuration = CreateConfiguration();
        var services = CreateServices(configuration);
        services.AddAI(configuration);

        Assert.Contains(services, d => d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(ForecastGenerationBackgroundWorker));
    }

    [Fact]
    public void DisabledAIOptions_DoNotRequireProviderSettings()
    {
        var result = new AIOptionsValidator().Validate(null, new AIOptions { Enabled = false });

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void EnabledAIOptions_RequireHttpsEndpointAndDeployments()
    {
        var validator = new AIOptionsValidator();
        var valid = new AIOptions
        {
            Enabled = true,
            Endpoint = "https://ai.example.invalid/",
            Deployment = "chat-model",
            EmbeddingDeployment = "embedding-model"
        };

        Assert.True(validator.Validate(null, valid).Succeeded);
        Assert.False(validator.Validate(null, new AIOptions { Enabled = true }).Succeeded);
        Assert.False(validator.Validate(null, new AIOptions
        {
            Enabled = true,
            Endpoint = valid.Endpoint,
            EmbeddingDeployment = valid.EmbeddingDeployment
        }).Succeeded);
        Assert.False(validator.Validate(null, new AIOptions
        {
            Enabled = true,
            Endpoint = valid.Endpoint,
            Deployment = valid.Deployment
        }).Succeeded);
        Assert.False(validator.Validate(null, new AIOptions
        {
            Enabled = true,
            Endpoint = "http://ai.example.invalid/",
            Deployment = "chat-model",
            EmbeddingDeployment = "embedding-model"
        }).Succeeded);
    }

    [Fact]
    public async Task DisabledAIRegistration_DoesNotCreateProviderClients_AndReturnsControlledFailure()
    {
        var configuration = CreateConfiguration();
        configuration["AI:Enabled"] = "false";
        configuration["AI:Endpoint"] = string.Empty;
        configuration["AI:Deployment"] = string.Empty;
        configuration["AI:EmbeddingDeployment"] = string.Empty;
        var services = CreateServices(configuration);
        services.AddSingleton<IValidateOptions<AIOptions>, AIOptionsValidator>();
        services.AddOptions<AIOptions>()
            .Bind(configuration.GetSection(AIOptions.SectionName))
            .ValidateOnStart();
        services.AddAI(configuration);

        using var provider = services.BuildServiceProvider();
        Assert.False(provider.GetRequiredService<IOptions<AIOptions>>().Value.Enabled);
        Assert.Null(provider.GetService<AzureOpenAIClient>());
        Assert.Null(provider.GetService<IChatClient>());
        Assert.Null(provider.GetService<IEmbeddingGenerator<string, Embedding<float>>>());
        Assert.IsType<DisabledIncidentAnalysisService>(
            provider.GetRequiredService<IIncidentAnalysisService>());
        Assert.IsType<DisabledIncidentVisualAnalysisService>(
            provider.GetRequiredService<IIncidentVisualAnalysisService>());
        Assert.IsType<DisabledIncidentEmbeddingService>(
            provider.GetRequiredService<IIncidentEmbeddingService>());
        Assert.IsType<DisabledDuplicateIncidentService>(
            provider.GetRequiredService<IDuplicateIncidentService>());
        Assert.IsType<DisabledSituationAnalysisService>(
            provider.GetRequiredService<ISituationAnalysisService>());

        var copilot = provider.GetRequiredService<IEmergencyCopilotService>();
        await Assert.ThrowsAsync<AIFeatureDisabledException>(
            () => copilot.QueryAsync(new CopilotQuery { Question = "test" }));
    }

    [Fact]
    public async Task DisabledAIException_IsMappedToServiceUnavailableProblem()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(
            NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(
            context,
            new AIFeatureDisabledException(),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, context.Response.StatusCode);
        context.Response.Body.Position = 0;
        using var body = await System.Text.Json.JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal("AI feature is disabled.", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public void Registration_UsesFinalHostOverridesWhenClientIsResolved()
    {
        var configuration = CreateConfiguration();
        configuration["AI:Endpoint"] = string.Empty;
        var services = CreateServices(configuration);
        services.AddAI(configuration);

        configuration["AI:Endpoint"] = "https://synthetic-provider.example.invalid/";
        using var provider = services.BuildServiceProvider();

        // Constructing the SDK client must use the later configuration; no
        // provider call or credential acquisition is made by this test.
        Assert.NotNull(provider.GetRequiredService<AzureOpenAIClient>());
    }

    [Fact]
    public void TestChatReplacement_DoesNotRequireARealProviderEndpoint()
    {
        var configuration = CreateConfiguration();
        configuration["AI:Endpoint"] = string.Empty;
        var services = CreateServices(configuration);
        services.AddAI(configuration);
        var substitute = Mock.Of<IChatClient>();
        services.RemoveAll<IChatClient>();
        services.AddSingleton(substitute);
        using var provider = services.BuildServiceProvider();

        Assert.Same(substitute, provider.GetRequiredService<IChatClient>());
    }

    [Theory]
    [InlineData("Endpoint", "AI endpoint is not configured.")]
    [InlineData("Deployment", "AI deployment is not configured.")]
    [InlineData("EmbeddingDeployment", "AI embedding deployment is not configured.")]
    public void MissingProviderSetting_FailsAtResolutionWithoutNetworkAccess(
        string setting, string expectedMessage)
    {
        var configuration = CreateConfiguration();
        configuration[$"AI:{setting}"] = string.Empty;
        var services = CreateServices(configuration);
        services.AddAI(configuration);
        using var provider = services.BuildServiceProvider();

        var error = Assert.Throws<InvalidOperationException>(
            () => provider.GetRequiredService<AzureOpenAIClient>());
        Assert.Equal(expectedMessage, error.Message);
    }

    private static IConfigurationRoot CreateConfiguration() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AI:Endpoint"] = "https://synthetic-provider.example.invalid/",
            ["AI:Deployment"] = "synthetic-chat",
            ["AI:EmbeddingDeployment"] = "synthetic-embedding"
        }).Build();

    private static ServiceCollection CreateServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddLogging();
        return services;
    }
}
