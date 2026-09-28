using System.Net;
using System.Net.Http.Json;
using EmergencyResponse.Application.Situations.DTOs;

namespace EmergencyResponse.Web.Services;

public sealed class SituationApiClient : ISituationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SituationApiClient> _logger;

    public SituationApiClient(
        HttpClient httpClient,
        ILogger<SituationApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OperationalSituationResponse>> GetOpenSituationsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/situations/open", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<OperationalSituationResponse>>(cancellationToken)
            ?? [];
    }

    public async Task<OperationalSituationResponse?> GetByIdAsync(
        Guid situationId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/situations/{situationId}", cancellationToken);

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
            .ReadFromJsonAsync<OperationalSituationResponse>(cancellationToken);
    }

    public async Task EvaluateAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("api/situations/evaluate", null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task ReviewAsync(
        Guid situationId,
        ReviewSituationRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/situations/{situationId}/review",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var msg = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(msg);
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task ResolveAsync(
        Guid situationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/situations/{situationId}/resolve",
            new SituationActionRequest { Notes = notes },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var msg = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(msg);
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task DismissAsync(
        Guid situationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/situations/{situationId}/dismiss",
            new SituationActionRequest { Notes = notes },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var msg = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(msg);
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task AcknowledgeAsync(
        Guid situationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/situations/{situationId}/acknowledge",
            new SituationActionRequest { Notes = notes },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var msg = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(msg);
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<SituationCommandCenterItem>> GetCommandCenterAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/situations/command-center", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        try
        {
            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiResponseWrapper<IReadOnlyList<SituationCommandCenterItem>>>(cancellationToken);
            if (wrapper?.Data != null)
            {
                return wrapper.Data;
            }
        }
        catch
        {
            // Fallback to direct deserialization
        }

        var direct = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<SituationCommandCenterItem>>(cancellationToken);
        return direct ?? [];
    }

    public async Task<IReadOnlyList<SituationMapItem>> GetOpenForMapAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/situations/open/map", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        try
        {
            var wrapper = await response.Content
                .ReadFromJsonAsync<ApiResponseWrapper<IReadOnlyList<SituationMapItem>>>(cancellationToken);
            if (wrapper?.Data != null)
            {
                return wrapper.Data;
            }
        }
        catch
        {
            // Fallback to direct deserialization
        }

        var direct = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<SituationMapItem>>(cancellationToken);
        return direct ?? [];
    }

    private sealed class ApiResponseWrapper<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
    }
}
