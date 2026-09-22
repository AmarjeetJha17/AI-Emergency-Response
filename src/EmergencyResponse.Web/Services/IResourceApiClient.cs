using EmergencyResponse.Application.Resources;

namespace EmergencyResponse.Web.Services;

public interface IResourceApiClient
{
    Task<IReadOnlyList<ResourceDetailsResponse>> GetResourcesAsync(
        CancellationToken cancellationToken = default);

    Task<ResourceDetailsResponse?> GetByIdAsync(
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceCommandCenterItem>> GetCommandCenterAsync(
        CancellationToken cancellationToken = default);

    Task<ResourceSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AssignableIncidentItem>> GetAssignableIncidentsAsync(
        Guid? resourceId = null,
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

    Task<ResourceRecommendationResponse> GetRecommendationsAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default);
}
