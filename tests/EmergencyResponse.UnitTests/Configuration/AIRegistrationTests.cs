using Azure.AI.OpenAI;
using EmergencyResponse.Infrastructure.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace EmergencyResponse.UnitTests.Configuration;

public sealed class AIRegistrationTests
{
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
