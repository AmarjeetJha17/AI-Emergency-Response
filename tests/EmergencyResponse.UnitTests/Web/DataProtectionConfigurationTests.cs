using EmergencyResponse.Web.Configuration;
using Microsoft.Extensions.Configuration;

namespace EmergencyResponse.UnitTests.Web;

public sealed class DataProtectionConfigurationTests
{
    [Fact]
    public void CompleteBlobAndKeyVaultConfiguration_UsesWrappedPersistenceByDefault()
    {
        var configuration = CreateConfiguration(
            ("DataProtection:BlobUri", "https://storage.blob.core.windows.net/web-data-protection/keys.xml"),
            ("DataProtection:KeyIdentifier", "https://vault.vault.azure.net/keys/web-data-protection/version"));

        var mode = DataProtectionConfiguration.ResolveMode(configuration, "Staging");

        Assert.Equal(DataProtectionMode.KeyVaultWrappedBlob, mode);
    }

    [Fact]
    public void MissingKeyVaultConfiguration_DoesNotSelectThePortfolioDemoFallback()
    {
        var configuration = CreateConfiguration(
            ("DataProtection:BlobUri", "https://storage.blob.core.windows.net/web-data-protection/keys.xml"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => DataProtectionConfiguration.ResolveMode(configuration, "PortfolioDemo"));

        Assert.Contains("must be configured together", exception.Message);
    }

    [Fact]
    public void ExplicitFallbackInPortfolioDemo_UsesBlobWithoutKeyVaultWrapping()
    {
        var configuration = CreateConfiguration(
            (DataProtectionConfiguration.PortfolioDemoFallbackSetting, "true"),
            ("DataProtection:BlobUri", "https://storage.blob.core.windows.net/web-data-protection/keys.xml"));

        var mode = DataProtectionConfiguration.ResolveMode(configuration, "PortfolioDemo");

        Assert.Equal(DataProtectionMode.PortfolioDemoUnwrappedBlob, mode);
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Development")]
    [InlineData("Test")]
    public void ExplicitFallbackOutsidePortfolioDemo_IsRejected(string environmentName)
    {
        var configuration = CreateConfiguration(
            (DataProtectionConfiguration.PortfolioDemoFallbackSetting, "true"),
            ("DataProtection:BlobUri", "https://storage.blob.core.windows.net/web-data-protection/keys.xml"));

        Assert.Throws<InvalidOperationException>(
            () => DataProtectionConfiguration.ResolveMode(configuration, environmentName));
    }

    [Fact]
    public void ProductionWithoutKeyVaultConfiguration_FailsClosed()
    {
        var configuration = CreateConfiguration();

        Assert.Throws<InvalidOperationException>(
            () => DataProtectionConfiguration.ResolveMode(configuration, "Production"));
    }

    [Fact]
    public void FallbackWithKeyVaultIdentifier_IsRejectedAsAmbiguous()
    {
        var configuration = CreateConfiguration(
            (DataProtectionConfiguration.PortfolioDemoFallbackSetting, "true"),
            ("DataProtection:BlobUri", "https://storage.blob.core.windows.net/web-data-protection/keys.xml"),
            ("DataProtection:KeyIdentifier", "https://vault.vault.azure.net/keys/web-data-protection/version"));

        var exception = Assert.Throws<InvalidOperationException>(
            () => DataProtectionConfiguration.ResolveMode(configuration, "PortfolioDemo"));

        Assert.Contains("Remove DataProtection:KeyIdentifier", exception.Message);
    }

    [Fact]
    public void DevelopmentWithoutHostedConfiguration_UsesFrameworkDefaults()
    {
        var configuration = CreateConfiguration();

        var mode = DataProtectionConfiguration.ResolveMode(configuration, "Development");

        Assert.Equal(DataProtectionMode.FrameworkDefault, mode);
    }

    private static IConfiguration CreateConfiguration(
        params (string Key, string Value)[] values)
    {
        var settings = values.ToDictionary(value => value.Key, value => (string?)value.Value);
        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}
