using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public static class ResourceCommandRules
{
    public static ResourceStatus GetTargetStatus(
        ResourceStatus current,
        ResourceCommand command)
    {
        return (current, command) switch
        {
            (ResourceStatus.Assigned, ResourceCommand.Dispatch)
                => ResourceStatus.Dispatched,

            (ResourceStatus.Dispatched, ResourceCommand.MarkEnRoute)
                => ResourceStatus.EnRoute,

            (ResourceStatus.EnRoute, ResourceCommand.MarkOnScene)
                => ResourceStatus.OnScene,

            (ResourceStatus.OnScene, ResourceCommand.BeginReturn)
                => ResourceStatus.Returning,

            (ResourceStatus.Returning, ResourceCommand.MarkAvailable)
                => ResourceStatus.Available,

            _ => throw new InvalidOperationException(
                $"Command {command} is not valid while resource is {current}.")
        };
    }
}
