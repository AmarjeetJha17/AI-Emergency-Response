namespace EmergencyResponse.Application.Security;

public sealed class SecurityOptions
{
    public const string SectionName = "Security";

    public bool MalwareScanningEnabled { get; set; }

    public bool FailClosedWhenScannerUnavailable { get; set; } = true;

    public string[] AllowedOrigins { get; set; } = [];

    public RateLimitOptions RateLimits { get; set; } = new();
}

public sealed class RateLimitOptions
{
    public int AuthenticationPerMinute { get; set; } = 5;

    public int IncidentCreatePerMinute { get; set; } = 10;

    public int MediaUploadPerMinute { get; set; } = 10;

    public int CopilotPerMinute { get; set; } = 30;

    public int AdminPerMinute { get; set; } = 60;
}
