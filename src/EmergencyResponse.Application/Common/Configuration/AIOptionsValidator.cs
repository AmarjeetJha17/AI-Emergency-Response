using Microsoft.Extensions.Options;

namespace EmergencyResponse.Application.Common.Configuration;

public sealed class AIOptionsValidator : IValidateOptions<AIOptions>
{
    public ValidateOptionsResult Validate(string? name, AIOptions options)
    {
        if (!options.Enabled)
        {
            return ValidateOptionsResult.Success;
        }

        if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps)
        {
            return ValidateOptionsResult.Fail(
                "AI endpoint must be an absolute HTTPS URI when AI is enabled.");
        }

        if (string.IsNullOrWhiteSpace(options.Deployment) ||
            string.IsNullOrWhiteSpace(options.EmbeddingDeployment))
        {
            return ValidateOptionsResult.Fail(
                "AI chat and embedding deployment names are required when AI is enabled.");
        }

        return ValidateOptionsResult.Success;
    }
}
