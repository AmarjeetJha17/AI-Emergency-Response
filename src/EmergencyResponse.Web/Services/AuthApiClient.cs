using System.Net;
using System.Net.Http.Json;

using EmergencyResponse.Application.DTOs.Authentication;

namespace EmergencyResponse.Web.Services;

public sealed class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AuthApiClient> _logger;

    public AuthApiClient(
        HttpClient httpClient,
        ILogger<AuthApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserResponse?> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/register",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new InvalidOperationException(
                "A user with this email already exists.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Registration failed with status code {StatusCode}",
                response.StatusCode);

            throw new InvalidOperationException(
                "Registration could not be completed.");
        }

        return await response.Content
            .ReadFromJsonAsync<UserResponse>(
                cancellationToken);
    }

    public async Task<AuthenticationResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login",
            request,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException(
                "Invalid email or password.");
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Login failed with status code {StatusCode}",
                response.StatusCode);

            throw new InvalidOperationException(
                "Login could not be completed.");
        }

        return await response.Content
            .ReadFromJsonAsync<AuthenticationResponse>(
                cancellationToken);
    }
}