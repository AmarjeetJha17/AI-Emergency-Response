using EmergencyResponse.Domain.Enums;

namespace EmergencyResponse.Application.Resources;

public interface IResourceService
{
    Task<IReadOnlyList<ResourceDetailsResponse>> GetResourcesAsync(
        CancellationToken cancellationToken = default);

    Task<ResourceDetailsResponse?> GetByIdAsync(
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<ResourceAssignmentResult> AssignAsync(
        Guid resourceId,
        Guid incidentId,
        string? notes,
        CancellationToken cancellationToken = default);

    Task<ResourceStatusTransitionResult> ChangeStatusAsync(
        Guid resourceId,
        ResourceStatus newStatus,
        Guid? incidentId,
        string? reason,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        Guid resourceId,
        string? notes,
        CancellationToken cancellationToken = default);
}
