namespace EmergencyResponse.Application.Reliability;

public sealed record AIServiceHealthSnapshot
{
    public DependencyHealthState State { get; init; }
    public int ConsecutiveFailures { get; init; }
    public DateTimeOffset? LastSuccessAtUtc { get; init; }
    public DateTimeOffset? LastFailureAtUtc { get; init; }
    public string? LastFailureCategory { get; init; }
}
