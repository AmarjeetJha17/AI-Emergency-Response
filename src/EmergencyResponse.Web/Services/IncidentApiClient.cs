using System.Net;
using System.Net.Http.Json;

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
        CancellationToken cancellationToken = default)
    {
        var query =
            $"api/incidents?page={page}" +
            $"&pageSize={pageSize}";

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
}