using EmergencyResponse.Application.Resources;

namespace EmergencyResponse.Web.Services;

public interface IResourceApiClient
{
    Task<IReadOnlyList<ResourceDetailsResponse>> GetResourcesAsync(
        CancellationToken cancellationToken = default);

    Task<ResourceDetailsResponse?> GetByIdAsync(
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<ResourceAssignmentResult> AssignAsync(
        Guid resourceId,
        AssignResourceRequest request,
        CancellationToken cancellationToken = default);

    Task<ResourceStatusTransitionResult> ChangeStatusAsync(
        Guid resourceId,
        ChangeResourceStatusRequest request,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        Guid resourceId,
        ReleaseResourceRequest request,
        CancellationToken cancellationToken = default);
}
