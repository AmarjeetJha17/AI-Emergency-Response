namespace EmergencyResponse.Application.Reliability;

public sealed class AICircuitBreakerOptions
{
    public const string SectionName = "Reliability:AI:CircuitBreaker";
    public int FailureThreshold { get; set; } = 5;
    public int OpenDurationSeconds { get; set; } = 30;
}
