using Azure.Identity;
using Microsoft.AspNetCore.DataProtection;

namespace EmergencyResponse.Web.Configuration;

public enum DataProtectionMode
{
    FrameworkDefault,
    KeyVaultWrappedBlob,
    PortfolioDemoUnwrappedBlob
}

public static class DataProtectionConfiguration
{
    public const string ApplicationName = "EmergencyResponse.Web";
    public const string PortfolioDemoEnvironment = "PortfolioDemo";
    public const string PortfolioDemoFallbackSetting =
        "DataProtection:UsePortfolioDemoUnwrappedKeys";

    public static DataProtectionMode Configure(
        IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        var mode = ResolveMode(configuration, environmentName);
        var dataProtection = services.AddDataProtection()
            .SetApplicationName(ApplicationName);

        if (mode is DataProtectionMode.KeyVaultWrappedBlob or
            DataProtectionMode.PortfolioDemoUnwrappedBlob)
        {
            var blobUri = configuration["DataProtection:BlobUri"]!;
            var credential = new DefaultAzureCredential();
            dataProtection.PersistKeysToAzureBlobStorage(new Uri(blobUri), credential);

            if (mode is DataProtectionMode.KeyVaultWrappedBlob)
            {
                dataProtection.ProtectKeysWithAzureKeyVault(
                    new Uri(configuration["DataProtection:KeyIdentifier"]!),
                    credential);
            }
        }

        return mode;
    }

    public static DataProtectionMode ResolveMode(
        IConfiguration configuration,
        string environmentName)
    {
        var fallbackValue = configuration[PortfolioDemoFallbackSetting];
        var fallbackRequested = false;
        if (!string.IsNullOrWhiteSpace(fallbackValue) &&
            !bool.TryParse(fallbackValue, out fallbackRequested))
        {
            throw new InvalidOperationException(
                $"{PortfolioDemoFallbackSetting} must be 'true' or 'false'.");
        }

        var blobUri = configuration["DataProtection:BlobUri"];
        var keyIdentifier = configuration["DataProtection:KeyIdentifier"];
        var hasBlobUri = !string.IsNullOrWhiteSpace(blobUri);
        var hasKeyIdentifier = !string.IsNullOrWhiteSpace(keyIdentifier);

        if (fallbackRequested)
        {
            if (!string.Equals(
                    environmentName,
                    PortfolioDemoEnvironment,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"{PortfolioDemoFallbackSetting}=true is allowed only in the {PortfolioDemoEnvironment} environment.");
            }

            if (!hasBlobUri)
            {
                throw new InvalidOperationException(
                    "DataProtection:BlobUri is required for the PortfolioDemo unwrapped-key mode.");
            }

            if (hasKeyIdentifier)
            {
                throw new InvalidOperationException(
                    "Remove DataProtection:KeyIdentifier when explicitly selecting the PortfolioDemo unwrapped-key mode.");
            }

            return DataProtectionMode.PortfolioDemoUnwrappedBlob;
        }

        if (hasBlobUri != hasKeyIdentifier)
        {
            throw new InvalidOperationException(
                "DataProtection:BlobUri and DataProtection:KeyIdentifier must be configured together unless the explicit PortfolioDemo fallback is enabled.");
        }

        if (hasBlobUri)
        {
            return DataProtectionMode.KeyVaultWrappedBlob;
        }

        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(environmentName, "Test", StringComparison.OrdinalIgnoreCase))
        {
            return DataProtectionMode.FrameworkDefault;
        }

        throw new InvalidOperationException(
            "Data Protection requires Blob Storage and Key Vault configuration outside Development/Test, or the explicit PortfolioDemo-only fallback.");
    }
}
