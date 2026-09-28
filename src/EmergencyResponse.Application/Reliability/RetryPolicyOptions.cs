namespace EmergencyResponse.Application.Reliability;

public sealed class RetryPolicyOptions
{
    public const string SectionName = "Reliability:RetryPolicy";

    public int MaxAttempts { get; set; } = 3;

    public int InitialDelaySeconds { get; set; } = 2;

    public int MaximumDelaySeconds { get; set; } = 30;
}
