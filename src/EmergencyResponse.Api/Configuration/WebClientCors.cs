namespace EmergencyResponse.Api.Configuration;

public static class WebClientCors
{
    public const string PolicyName = "WebClient";

    public static IServiceCollection AddWebClientCors(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        var allowedOrigins = configuredOrigins.Length > 0
            ? configuredOrigins
            : environment.IsDevelopment() || environment.IsEnvironment("Test")
                ? ["http://localhost:5021", "https://localhost:7201"]
                : [];

        // CORS origins are exact authorities, not URL paths or wildcard patterns.
        // Reject invalid configuration instead of accidentally expanding trust.
        if (allowedOrigins.Any(origin => string.IsNullOrWhiteSpace(origin) || origin.Contains('*') ||
            !Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("https" or "http") || !string.IsNullOrEmpty(uri.UserInfo) ||
            origin != uri.GetLeftPart(UriPartial.Authority)))
        {
            throw new InvalidOperationException("Cors:AllowedOrigins must contain exact HTTP(S) origins without paths, trailing slashes or wildcards.");
        }

        services.AddCors(options => options.AddPolicy(PolicyName, policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            // SignalR's browser client sends credentials by default; restrict these
            // to the explicitly trusted Web origins (including affinity cookies).
            .AllowCredentials()));
        return services;
    }
}
