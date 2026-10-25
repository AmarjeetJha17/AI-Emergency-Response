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
