using System.Net;
using System.Net.Http.Json;
using EmergencyResponse.Application.Notifications.DTOs;

namespace EmergencyResponse.Web.Services;

public sealed class NotificationApiClient : INotificationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationApiClient> _logger;

    public NotificationApiClient(
        HttpClient httpClient,
        ILogger<NotificationApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<NotificationItem>> GetForCurrentUserAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/notifications?take={take}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<IReadOnlyList<NotificationItem>>(cancellationToken)
            ?? [];
    }

    public async Task<NotificationSummary?> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync("api/notifications/summary", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return new NotificationSummary();
        }

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<NotificationSummary>(cancellationToken);
    }

    public async Task MarkAsReadAsync(
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/notifications/{notificationId}/read", null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();
    }

    public async Task MarkAllAsReadAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("api/notifications/read-all", null, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        response.EnsureSuccessStatusCode();
    }
}
