using EmergencyResponse.Application.DTOs.Authentication;

namespace EmergencyResponse.Web.Services;

public interface IAuthApiClient
{
    Task<UserResponse?> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthenticationResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}