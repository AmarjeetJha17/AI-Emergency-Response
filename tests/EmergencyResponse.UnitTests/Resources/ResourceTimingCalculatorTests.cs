using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Entities;
using EmergencyResponse.Domain.Enums;
using Xunit;

namespace EmergencyResponse.UnitTests.Resources;

public sealed class ResourceTimingCalculatorTests
{
    [Fact]
    public void Calculate_NullAssignment_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ResourceTimingCalculator.Calculate(null!));
    }

    [Fact]
    public void Calculate_AllTimestampsPresent_ComputesAllMetricsAccurately()
    {
        var baseTime = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);
        var assignment = new ResourceAssignment
        {
            Id = Guid.NewGuid(),
            IncidentId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            Status = ResourceAssignmentStatus.Completed,
            AssignedAtUtc = baseTime,
            DispatchedAtUtc = baseTime.AddMinutes(2),
            EnRouteAtUtc = baseTime.AddMinutes(5),
            OnSceneAtUtc = baseTime.AddMinutes(15),
            ReturningAtUtc = baseTime.AddMinutes(45),
            ReleasedAtUtc = baseTime.AddMinutes(60)
        };

        var timing = ResourceTimingCalculator.Calculate(assignment);

        Assert.NotNull(timing);
        Assert.Equal(TimeSpan.FromMinutes(2), timing.AssignmentToDispatch);
        Assert.Equal(TimeSpan.FromMinutes(3), timing.DispatchToEnRoute);
        Assert.Equal(TimeSpan.FromMinutes(10), timing.EnRouteToOnScene);
        Assert.Equal(TimeSpan.FromMinutes(30), timing.OnSceneDuration);
        Assert.Equal(TimeSpan.FromMinutes(60), timing.TotalDeploymentDuration);
    }

    [Fact]
    public void Calculate_PartialTimestamps_ComputesOnlyAvailableMetrics()
    {
        var baseTime = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);
        var assignment = new ResourceAssignment
        {
            Id = Guid.NewGuid(),
            IncidentId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            Status = ResourceAssignmentStatus.Active,
            AssignedAtUtc = baseTime,
            DispatchedAtUtc = baseTime.AddMinutes(3),
            EnRouteAtUtc = null,
            OnSceneAtUtc = null,
            ReturningAtUtc = null,
            ReleasedAtUtc = null
        };

        var timing = ResourceTimingCalculator.Calculate(assignment);

        Assert.NotNull(timing);
        Assert.Equal(TimeSpan.FromMinutes(3), timing.AssignmentToDispatch);
        Assert.Null(timing.DispatchToEnRoute);
        Assert.Null(timing.EnRouteToOnScene);
        Assert.Null(timing.OnSceneDuration);
        Assert.Null(timing.TotalDeploymentDuration);
    }

    [Fact]
    public void Calculate_MissingDispatchedAtUtc_HandlesGracefully()
    {
        var baseTime = new DateTime(2026, 9, 25, 10, 0, 0, DateTimeKind.Utc);
        var assignment = new ResourceAssignment
        {
            Id = Guid.NewGuid(),
            IncidentId = Guid.NewGuid(),
            ResourceId = Guid.NewGuid(),
            Status = ResourceAssignmentStatus.Active,
            AssignedAtUtc = baseTime,
            DispatchedAtUtc = null
        };

        var timing = ResourceTimingCalculator.Calculate(assignment);

        Assert.NotNull(timing);
        Assert.Null(timing.AssignmentToDispatch);
        Assert.Null(timing.DispatchToEnRoute);
        Assert.Null(timing.EnRouteToOnScene);
        Assert.Null(timing.OnSceneDuration);
        Assert.Null(timing.TotalDeploymentDuration);
    }
}
