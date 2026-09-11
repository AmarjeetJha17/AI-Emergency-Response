using EmergencyResponse.Application.DTOs.Authentication;

namespace EmergencyResponse.Application.Interfaces;

public interface IAuthenticationService
{
    Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthenticationResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}