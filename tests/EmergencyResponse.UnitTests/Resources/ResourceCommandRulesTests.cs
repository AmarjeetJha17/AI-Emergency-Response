using EmergencyResponse.Application.Resources;
using EmergencyResponse.Domain.Enums;
using Xunit;

namespace EmergencyResponse.UnitTests.Resources;

[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Unit)]
public sealed class ResourceCommandRulesTests
{
    [Theory]
    [InlineData(ResourceStatus.Assigned, ResourceCommand.Dispatch, ResourceStatus.Dispatched)]
    [InlineData(ResourceStatus.Dispatched, ResourceCommand.MarkEnRoute, ResourceStatus.EnRoute)]
    [InlineData(ResourceStatus.EnRoute, ResourceCommand.MarkOnScene, ResourceStatus.OnScene)]
    [InlineData(ResourceStatus.OnScene, ResourceCommand.BeginReturn, ResourceStatus.Returning)]
    [InlineData(ResourceStatus.Returning, ResourceCommand.MarkAvailable, ResourceStatus.Available)]
    public void GetTargetStatus_ValidCommand_ReturnsExpectedTargetStatus(
        ResourceStatus current,
        ResourceCommand command,
        ResourceStatus expected)
    {
        var result = ResourceCommandRules.GetTargetStatus(current, command);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(ResourceStatus.Available, ResourceCommand.Dispatch)]
    [InlineData(ResourceStatus.Available, ResourceCommand.MarkEnRoute)]
    [InlineData(ResourceStatus.Available, ResourceCommand.MarkOnScene)]
    [InlineData(ResourceStatus.Available, ResourceCommand.BeginReturn)]
    [InlineData(ResourceStatus.Available, ResourceCommand.MarkAvailable)]
    [InlineData(ResourceStatus.Assigned, ResourceCommand.MarkEnRoute)]
    [InlineData(ResourceStatus.Assigned, ResourceCommand.MarkOnScene)]
    [InlineData(ResourceStatus.Assigned, ResourceCommand.BeginReturn)]
    [InlineData(ResourceStatus.Assigned, ResourceCommand.MarkAvailable)]
    [InlineData(ResourceStatus.Dispatched, ResourceCommand.Dispatch)]
    [InlineData(ResourceStatus.Dispatched, ResourceCommand.MarkOnScene)]
    [InlineData(ResourceStatus.Dispatched, ResourceCommand.BeginReturn)]
    [InlineData(ResourceStatus.Dispatched, ResourceCommand.MarkAvailable)]
    [InlineData(ResourceStatus.EnRoute, ResourceCommand.Dispatch)]
    [InlineData(ResourceStatus.EnRoute, ResourceCommand.MarkEnRoute)]
    [InlineData(ResourceStatus.EnRoute, ResourceCommand.BeginReturn)]
    [InlineData(ResourceStatus.EnRoute, ResourceCommand.MarkAvailable)]
    [InlineData(ResourceStatus.OnScene, ResourceCommand.Dispatch)]
    [InlineData(ResourceStatus.OnScene, ResourceCommand.MarkEnRoute)]
    [InlineData(ResourceStatus.OnScene, ResourceCommand.MarkOnScene)]
    [InlineData(ResourceStatus.OnScene, ResourceCommand.MarkAvailable)]
    [InlineData(ResourceStatus.Returning, ResourceCommand.Dispatch)]
    [InlineData(ResourceStatus.Returning, ResourceCommand.MarkEnRoute)]
    [InlineData(ResourceStatus.Returning, ResourceCommand.MarkOnScene)]
    [InlineData(ResourceStatus.Returning, ResourceCommand.BeginReturn)]
    public void GetTargetStatus_InvalidCommand_ThrowsInvalidOperationException(
        ResourceStatus current,
        ResourceCommand command)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => ResourceCommandRules.GetTargetStatus(current, command));

        Assert.Contains(command.ToString(), ex.Message);
        Assert.Contains(current.ToString(), ex.Message);
    }
}
