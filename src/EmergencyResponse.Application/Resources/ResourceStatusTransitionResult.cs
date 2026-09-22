using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public sealed record ResourceStatusTransitionResult
{
    public Guid ResourceId { get; init; }

    public ResourceStatus PreviousStatus { get; init; }

    public ResourceStatus NewStatus { get; init; }

    public DateTime ChangedAtUtc { get; init; }
}
