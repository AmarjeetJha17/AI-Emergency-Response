using EmergencyResponse.Domain.Entities;

namespace EmergencyResponse.Application.Resources;

public static class ResourceTimingCalculator
{
    public static ResourceOperationalTiming Calculate(ResourceAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return new ResourceOperationalTiming
        {
            AssignmentToDispatch =
                assignment.DispatchedAtUtc.HasValue
                    ? assignment.DispatchedAtUtc.Value - assignment.AssignedAtUtc
                    : null,

            DispatchToEnRoute =
                assignment.DispatchedAtUtc.HasValue && assignment.EnRouteAtUtc.HasValue
                    ? assignment.EnRouteAtUtc.Value - assignment.DispatchedAtUtc.Value
                    : null,

            EnRouteToOnScene =
                assignment.EnRouteAtUtc.HasValue && assignment.OnSceneAtUtc.HasValue
                    ? assignment.OnSceneAtUtc.Value - assignment.EnRouteAtUtc.Value
                    : null,

            OnSceneDuration =
                assignment.OnSceneAtUtc.HasValue && assignment.ReturningAtUtc.HasValue
                    ? assignment.ReturningAtUtc.Value - assignment.OnSceneAtUtc.Value
                    : null,

            TotalDeploymentDuration =
                assignment.AssignedAtUtc != default && assignment.ReleasedAtUtc.HasValue
                    ? assignment.ReleasedAtUtc.Value - assignment.AssignedAtUtc
                    : null
        };
    }
}
