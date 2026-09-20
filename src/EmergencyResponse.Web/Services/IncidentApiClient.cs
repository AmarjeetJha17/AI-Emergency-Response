using System.Net;
using System.Net.Http.Json;

using EmergencyResponse.Application.AI.DTOs;
using EmergencyResponse.Application.DTOs.Common;
using EmergencyResponse.Application.DTOs.Incidents;

namespace EmergencyResponse.Web.Services;

public sealed class IncidentApiClient : IIncidentApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IncidentApiClient> _logger;

    public IncidentApiClient(
        HttpClient httpClient,
        ILogger<IncidentApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IncidentResponse?> CreateAsync(
        CreateIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/incidents",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Incident creation failed. Status: {StatusCode}",
                response.StatusCode);

            throw new InvalidOperationException(
                "Unable to create incident.");
        }

        return await response.Content
            .ReadFromJsonAsync<IncidentResponse>(
                cancellationToken);
    }

    public async Task<IReadOnlyList<DuplicateIncidentCandidate>>
        GetDuplicatesAsync(
            Guid incidentId,
            CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/incidents/{incidentId}/duplicates",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        var envelope =
            await response.Content.ReadFromJsonAsync<
                ApiResponseEnvelope<IReadOnlyList<DuplicateIncidentCandidate>>>(
                cancellationToken);

        return envelope?.Data ?? Array.Empty<DuplicateIncidentCandidate>();
    }

    public async Task ReviewDuplicateAsync(
        Guid incidentId,
        Guid matchId,
        ReviewDuplicateIncidentRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/incidents/{incidentId}/duplicates/{matchId}/review",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<IncidentResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/incidents/{id}",
            cancellationToken);

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
            .ReadFromJsonAsync<IncidentResponse>(
                cancellationToken);
    }

    public async Task<PagedResult<IncidentListItem>?> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null,
        Guid? incidentTypeId = null,
        string? status = null,
        string? severity = null,
        bool myReportsOnly = false,
        CancellationToken cancellationToken = default)
    {
        var query =
            $"api/incidents?page={page}" +
            $"&pageSize={pageSize}";

        if (myReportsOnly)
        {
            query += "&myReportsOnly=true";
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query +=
                $"&search={Uri.EscapeDataString(search)}";
        }

        if (incidentTypeId.HasValue)
        {
            query +=
                $"&incidentTypeId={incidentTypeId}";
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query +=
                $"&status={Uri.EscapeDataString(status)}";
        }

        if (!string.IsNullOrWhiteSpace(severity))
        {
            query +=
                $"&severity={Uri.EscapeDataString(severity)}";
        }

        var response = await _httpClient.GetAsync(
            query,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<PagedResult<IncidentListItem>>(
                cancellationToken);
    }

    public async Task ReviewVisualAnalysisAsync(
        Guid incidentId,
        Guid mediaId,
        ReviewVisualAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/incidents/{incidentId}/media/{mediaId}/analysis/review",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();
    }

    private sealed class ApiResponseEnvelope<T>
    {
        public T? Data { get; init; }
    }

    public async Task<IReadOnlyList<IncidentMediaResponse>> GetMediaAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/incidents/{incidentId}/media",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Array.Empty<IncidentMediaResponse>();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<IncidentMediaResponse>>(
                cancellationToken)
            ?? Array.Empty<IncidentMediaResponse>();
    }

    public async Task<VisualAnalysisResponse?> GetVisualAnalysisAsync(
        Guid incidentId,
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/incidents/{incidentId}/media/{mediaId}/analysis",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<VisualAnalysisResponse>(
                cancellationToken);
    }

    public async Task<MediaStreamResult?> GetMediaStreamAsync(
        Guid incidentId,
        Guid mediaId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/incidents/{incidentId}/media/{mediaId}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var contentType =
            response.Content.Headers.ContentType?.MediaType
            ?? "application/octet-stream";

        var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken);

        return new MediaStreamResult(stream, contentType);
    }

    public async Task<MediaStreamResult?> GetFrameAsync(
        Guid incidentId,
        Guid mediaId,
        int frameNumber,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"api/incidents/{incidentId}/media/{mediaId}/analysis/frames/{frameNumber}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var contentType =
            response.Content.Headers.ContentType?.MediaType
            ?? "image/jpeg";

        var stream = await response.Content
            .ReadAsStreamAsync(cancellationToken);

        return new MediaStreamResult(stream, contentType);
    }

    public async Task<IncidentResponse?> UpdateStatusAsync(
        Guid id,
        UpdateIncidentStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PatchAsJsonAsync(
            $"api/incidents/{id}/status",
            request,
            cancellationToken);

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
            .ReadFromJsonAsync<IncidentResponse>(
                cancellationToken);
    }
}