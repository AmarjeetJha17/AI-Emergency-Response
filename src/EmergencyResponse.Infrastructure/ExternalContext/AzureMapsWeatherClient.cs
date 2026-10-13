using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using EmergencyResponse.Application.ExternalContext;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmergencyResponse.Infrastructure.ExternalContext;

public sealed class AzureMapsWeatherClient : IWeatherContextClient
{
    private const string Scope = "https://atlas.microsoft.com/.default";
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly AzureMapsOptions _options;
    private readonly TokenCredential _credential;
    private readonly ILogger<AzureMapsWeatherClient> _logger;

    public AzureMapsWeatherClient(
        HttpClient httpClient,
        IMemoryCache cache,
        IOptions<AzureMapsOptions> options,
        ILogger<AzureMapsWeatherClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _options = options.Value;
        _credential = new DefaultAzureCredential();
        _logger = logger;
    }

    public async Task<WeatherContextData?> GetCurrentAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return null;
        }

        var key = $"azuremaps:weather:{latitude:F4}:{longitude:F4}";
        if (_cache.TryGetValue(key, out WeatherContextData? cached))
        {
            return cached;
        }

        try
        {
            using var document = await GetJsonAsync(
                $"/weather/currentConditions/json?api-version=1.1&unit=metric&details=true&query={Query(latitude, longitude)}",
                cancellationToken);
            var item = FirstResult(document);
            if (item is null)
            {
                return null;
            }
            var current = item.Value;

            var result = new WeatherContextData
            {
                ObservedAtUtc = ReadDate(current, "dateTime") ?? DateTime.UtcNow,
                TemperatureC = ReadDouble(current, "temperature", "value"),
                ApparentTemperatureC = ReadDouble(current, "realFeelTemperature", "value"),
                RelativeHumidity = ReadDouble(current, "relativeHumidity"),
                WindSpeedKph = ReadDouble(current, "wind", "speed", "value"),
                WindDirectionDegrees = ReadDouble(current, "wind", "direction", "degrees"),
                PrecipitationMm = ReadDouble(current, "precipitationSummary", "pastHour", "value"),
                VisibilityKm = ReadDouble(current, "visibility", "value"),
                PressureHpa = ReadDouble(current, "pressure", "value"),
                CloudCoverPercent = ReadDouble(current, "cloudCover"),
                WeatherPhrase = ReadString(current, "phrase")
            };
            _cache.Set(key, result, TimeSpan.FromMinutes(_options.WeatherCacheMinutes));
            return result;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or CredentialUnavailableException)
        {
            _logger.LogWarning(ex, "Azure Maps weather context is unavailable.");
            return null;
        }
    }

    public Task<IReadOnlyList<WeatherForecastPoint>> GetHourlyForecastAsync(
        double latitude,
        double longitude,
        int durationHours,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<WeatherForecastPoint>>([]);

    public async Task<IReadOnlyList<SevereWeatherAlert>> GetSevereAlertsAsync(
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return [];
        }

        var key = $"azuremaps:alerts:{latitude:F4}:{longitude:F4}";
        if (_cache.TryGetValue(key, out IReadOnlyList<SevereWeatherAlert>? cached))
        {
            return cached ?? [];
        }

        try
        {
            using var document = await GetJsonAsync(
                $"/weather/severe/alerts/json?api-version=1.1&query={Query(latitude, longitude)}",
                cancellationToken);
            var alerts = document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.EnumerateArray()
                : document.RootElement.TryGetProperty("results", out var results)
                    ? results.EnumerateArray()
                    : [];
            var mapped = alerts.Select(x => new SevereWeatherAlert
            {
                AlertId = ReadString(x, "alertId") ?? ReadString(x, "id") ?? string.Empty,
                Type = ReadString(x, "type") ?? string.Empty,
                Category = ReadString(x, "category") ?? string.Empty,
                Severity = ReadString(x, "severity"),
                Description = ReadString(x, "description"),
                StartTimeUtc = ReadDate(x, "startTime"),
                EndTimeUtc = ReadDate(x, "endTime"),
                Source = ReadString(x, "source")
            }).ToArray();
            _cache.Set(key, mapped, TimeSpan.FromMinutes(_options.SevereAlertCacheMinutes));
            return mapped;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or CredentialUnavailableException)
        {
            _logger.LogWarning(ex, "Azure Maps severe weather alerts are unavailable.");
            return [];
        }
    }

    private async Task<JsonDocument> GetJsonAsync(
        string path,
        CancellationToken cancellationToken)
    {
        var token = await _credential.GetTokenAsync(
            new TokenRequestContext([Scope]),
            cancellationToken);
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            new Uri(new Uri(_options.Endpoint.TrimEnd('/') + "/"), path.TrimStart('/')));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    private static string Query(double latitude, double longitude)
        => Uri.EscapeDataString(
            $"{latitude.ToString(CultureInfo.InvariantCulture)}," +
            $"{longitude.ToString(CultureInfo.InvariantCulture)}");

    private static JsonElement? FirstResult(JsonDocument document)
    {
        if (document.RootElement.TryGetProperty("results", out var results) &&
            results.ValueKind == JsonValueKind.Array &&
            results.GetArrayLength() > 0)
        {
            return results[0];
        }

        return document.RootElement.ValueKind == JsonValueKind.Object
            ? document.RootElement
            : null;
    }

    private static string? ReadString(JsonElement element, params string[] path)
    {
        var value = Navigate(element, path);
        return value?.ValueKind == JsonValueKind.String ? value.Value.GetString() : null;
    }

    private static double? ReadDouble(JsonElement element, params string[] path)
    {
        var value = Navigate(element, path);
        return value?.ValueKind == JsonValueKind.Number && value.Value.TryGetDouble(out var number)
            ? number
            : null;
    }

    private static DateTime? ReadDate(JsonElement element, params string[] path)
    {
        var value = ReadString(element, path);
        return DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var date)
            ? date
            : null;
    }

    private static JsonElement? Navigate(JsonElement element, IReadOnlyList<string> path)
    {
        foreach (var part in path)
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(part, out element))
            {
                return null;
            }
        }

        return element;
    }
}
