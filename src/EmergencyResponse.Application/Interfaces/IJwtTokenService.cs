using EmergencyResponse.Application.DTOs.Authentication;

namespace EmergencyResponse.Application.Interfaces;

public interface IJwtTokenService
{
    Task<(string Token, DateTime ExpiresAtUtc)> GenerateTokenAsync(
        AuthenticationUserInfo user);
}