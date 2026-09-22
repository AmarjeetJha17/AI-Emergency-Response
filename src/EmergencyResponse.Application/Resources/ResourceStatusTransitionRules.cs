using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public static class ResourceStatusTransitionRules
{
    public static bool IsAllowed(ResourceStatus current, ResourceStatus next)
    {
        return current switch
        {
            ResourceStatus.Available =>
                next is
                    ResourceStatus.Assigned or
                    ResourceStatus.Unavailable or
                    ResourceStatus.Maintenance,

            ResourceStatus.Assigned =>
                next is
                    ResourceStatus.Available or
                    ResourceStatus.Dispatched or
                    ResourceStatus.Unavailable,

            ResourceStatus.Dispatched =>
                next is
                    ResourceStatus.EnRoute or
                    ResourceStatus.Available,

            ResourceStatus.EnRoute =>
                next is
                    ResourceStatus.OnScene or
                    ResourceStatus.Available,

            ResourceStatus.OnScene =>
                next is
                    ResourceStatus.Returning,

            ResourceStatus.Returning =>
                next is
                    ResourceStatus.Available or
                    ResourceStatus.Maintenance,

            ResourceStatus.Unavailable =>
                next is
                    ResourceStatus.Available,

            ResourceStatus.Maintenance =>
                next is
                    ResourceStatus.Available or
                    ResourceStatus.Unavailable,

            _ => false
        };
    }
}
