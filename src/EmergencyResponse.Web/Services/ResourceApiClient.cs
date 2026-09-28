using System.Net;
using System.Net.Http.Json;

using EmergencyResponse.Application.Resources;

namespace EmergencyResponse.Web.Services;

public sealed class ResourceApiClient : IResourceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ResourceApiClient> _logger;

    public ResourceApiClient(
        HttpClient httpClient,
        ILogger<ResourceApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ResourceDetailsResponse>> GetResourcesAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/resources", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<ResourceDetailsResponse>>(cancellationToken)
            ?? [];
    }

    public async Task<ResourceDetailsResponse?> GetByIdAsync(
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/resources/{resourceId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ResourceDetailsResponse>(cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceCommandCenterItem>> GetCommandCenterAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/resources/command-center", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<ResourceCommandCenterItem>>(cancellationToken)
            ?? [];
    }

    public async Task<ResourceSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/resources/summary", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<ResourceSummary>(cancellationToken)
            ?? new ResourceSummary();
    }

    public async Task<IReadOnlyList<AssignableIncidentItem>> GetAssignableIncidentsAsync(
        Guid? resourceId = null,
        CancellationToken cancellationToken = default)
    {
        var url = resourceId.HasValue
            ? $"api/resources/assignable-incidents?resourceId={resourceId.Value}"
            : "api/resources/assignable-incidents";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<AssignableIncidentItem>>(cancellationToken)
            ?? [];
    }

    public async Task<ResourceAssignmentResult> AssignAsync(
        Guid resourceId,
        AssignResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/resources/{resourceId}/assign",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Resource assignment failed: {err}", null, response.StatusCode);
        }

        var result = await response.Content
            .ReadFromJsonAsync<ResourceAssignmentResult>(cancellationToken);

        return result ?? throw new InvalidOperationException("Empty response received from assignment endpoint.");
    }

    public async Task<ResourceStatusTransitionResult> ChangeStatusAsync(
        Guid resourceId,
        ChangeResourceStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/resources/{resourceId}/status",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Resource status change failed: {err}", null, response.StatusCode);
        }

        var result = await response.Content
            .ReadFromJsonAsync<ResourceStatusTransitionResult>(cancellationToken);

        return result ?? throw new InvalidOperationException("Empty response received from status change endpoint.");
    }

    public async Task ReleaseAsync(
        Guid resourceId,
        ReleaseResourceRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/resources/{resourceId}/release",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Resource release failed: {err}", null, response.StatusCode);
        }
    }

    public async Task<ResourceRecommendationResponse> GetRecommendationsAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/resources/recommendations/{incidentId}",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Fetching recommendations failed: {err}", null, response.StatusCode);
        }

        var result = await response.Content
            .ReadFromJsonAsync<ResourceRecommendationResponse>(cancellationToken);

        return result ?? throw new InvalidOperationException("Empty response received from recommendations endpoint.");
    }

    public async Task<ResourceCommandResult> ExecuteCommandAsync(
        Guid resourceId,
        ResourceCommandRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/resources/{resourceId}/command",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflictMsg = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ResourceConcurrencyException(conflictMsg);
        }

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new HttpRequestException($"Resource command failed: {err}", null, response.StatusCode);
        }

        var result = await response.Content
            .ReadFromJsonAsync<ResourceCommandResult>(cancellationToken);

        return result ?? throw new InvalidOperationException("Empty response received from command endpoint.");
    }

    public async Task<IReadOnlyList<ResourceStatusHistoryItem>> GetHistoryAsync(
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/resources/{resourceId}/history",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new KeyNotFoundException("Resource not found.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<ResourceStatusHistoryItem>>(cancellationToken)
            ?? [];
    }
}
