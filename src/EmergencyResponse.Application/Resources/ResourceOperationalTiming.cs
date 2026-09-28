namespace EmergencyResponse.Application.Resources;

public sealed record ResourceOperationalTiming
{
    public TimeSpan? AssignmentToDispatch { get; init; }

    public TimeSpan? DispatchToEnRoute { get; init; }

    public TimeSpan? EnRouteToOnScene { get; init; }

    public TimeSpan? OnSceneDuration { get; init; }

    public TimeSpan? TotalDeploymentDuration { get; init; }
}
