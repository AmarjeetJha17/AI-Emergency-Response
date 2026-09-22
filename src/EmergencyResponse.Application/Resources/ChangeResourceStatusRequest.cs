using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public sealed record ChangeResourceStatusRequest
{
    public ResourceStatus Status { get; init; }

    public Guid? IncidentId { get; init; }

    public string? Reason { get; init; }
}
