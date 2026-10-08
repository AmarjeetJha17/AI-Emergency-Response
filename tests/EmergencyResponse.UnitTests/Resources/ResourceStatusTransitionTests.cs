using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Enums;
using Xunit;

namespace EmergencyResponse.UnitTests.Resources;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class ResourceStatusTransitionTests
{
    [Theory]
    [InlineData(ResourceStatus.Available, ResourceStatus.Assigned, true)]
    [InlineData(ResourceStatus.Available, ResourceStatus.Unavailable, true)]
    [InlineData(ResourceStatus.Available, ResourceStatus.Maintenance, true)]
    [InlineData(ResourceStatus.Assigned, ResourceStatus.Dispatched, true)]
    [InlineData(ResourceStatus.Assigned, ResourceStatus.Available, true)]
    [InlineData(ResourceStatus.Assigned, ResourceStatus.Unavailable, true)]
    [InlineData(ResourceStatus.Dispatched, ResourceStatus.EnRoute, true)]
    [InlineData(ResourceStatus.Dispatched, ResourceStatus.Available, true)]
    [InlineData(ResourceStatus.EnRoute, ResourceStatus.OnScene, true)]
    [InlineData(ResourceStatus.EnRoute, ResourceStatus.Available, true)]
    [InlineData(ResourceStatus.OnScene, ResourceStatus.Returning, true)]
    [InlineData(ResourceStatus.Returning, ResourceStatus.Available, true)]
    [InlineData(ResourceStatus.Returning, ResourceStatus.Maintenance, true)]
    [InlineData(ResourceStatus.Unavailable, ResourceStatus.Available, true)]
    [InlineData(ResourceStatus.Maintenance, ResourceStatus.Available, true)]
    [InlineData(ResourceStatus.Maintenance, ResourceStatus.Unavailable, true)]
    public void ExpectedTransition_IsAllowed(
        ResourceStatus current,
        ResourceStatus next,
        bool expected)
    {
        Assert.Equal(
            expected,
            ResourceStatusTransitionRules.IsAllowed(current, next));
    }

    [Theory]
    [InlineData(ResourceStatus.Available, ResourceStatus.OnScene)]
    [InlineData(ResourceStatus.Available, ResourceStatus.EnRoute)]
    [InlineData(ResourceStatus.Available, ResourceStatus.Returning)]
    [InlineData(ResourceStatus.Assigned, ResourceStatus.OnScene)]
    [InlineData(ResourceStatus.Assigned, ResourceStatus.Returning)]
    [InlineData(ResourceStatus.Dispatched, ResourceStatus.OnScene)]
    [InlineData(ResourceStatus.Dispatched, ResourceStatus.Maintenance)]
    [InlineData(ResourceStatus.EnRoute, ResourceStatus.Returning)]
    [InlineData(ResourceStatus.EnRoute, ResourceStatus.Maintenance)]
    [InlineData(ResourceStatus.OnScene, ResourceStatus.Dispatched)]
    [InlineData(ResourceStatus.OnScene, ResourceStatus.Available)]
    [InlineData(ResourceStatus.OnScene, ResourceStatus.Maintenance)]
    [InlineData(ResourceStatus.Returning, ResourceStatus.OnScene)]
    [InlineData(ResourceStatus.Returning, ResourceStatus.Dispatched)]
    public void InvalidTransition_IsRejected(
        ResourceStatus current,
        ResourceStatus next)
    {
        Assert.False(
            ResourceStatusTransitionRules.IsAllowed(current, next));
    }

    [Fact]
    public void FullOperationalLifecycle_ShouldBeValid()
    {
        var lifecycle = new[]
        {
            (ResourceStatus.Available, ResourceStatus.Assigned),
            (ResourceStatus.Assigned, ResourceStatus.Dispatched),
            (ResourceStatus.Dispatched, ResourceStatus.EnRoute),
            (ResourceStatus.EnRoute, ResourceStatus.OnScene),
            (ResourceStatus.OnScene, ResourceStatus.Returning),
            (ResourceStatus.Returning, ResourceStatus.Available)
        };

        foreach (var (from, to) in lifecycle)
        {
            Assert.True(
                ResourceStatusTransitionRules.IsAllowed(from, to),
                $"Transition from {from} to {to} should be allowed in the operational lifecycle.");
        }
    }
}
