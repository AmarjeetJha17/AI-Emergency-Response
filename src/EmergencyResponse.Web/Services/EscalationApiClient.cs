using System.Net;
using System.Net.Http.Json;
using EmergencyResponse.Application.Escalation.DTOs;

namespace EmergencyResponse.Web.Services;

public sealed class EscalationApiClient : IEscalationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EscalationApiClient> _logger;

    public EscalationApiClient(
        HttpClient httpClient,
        ILogger<EscalationApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<IncidentEscalationResponse>> GetOpenEscalationsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/escalations/open", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<IncidentEscalationResponse>>(cancellationToken)
            ?? [];
    }

    public async Task<IReadOnlyList<IncidentEscalationResponse>> GetByIncidentIdAsync(
        Guid incidentId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/escalations/incident/{incidentId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<IncidentEscalationResponse>>(cancellationToken)
            ?? [];
    }

    public async Task AcknowledgeAsync(
        Guid escalationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/escalations/{escalationId}/acknowledge",
            new EscalationActionRequest { Notes = notes },
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
        Guid escalationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/escalations/{escalationId}/resolve",
            new EscalationActionRequest { Notes = notes },
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
        Guid escalationId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/escalations/{escalationId}/dismiss",
            new EscalationActionRequest { Notes = notes },
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
}
