namespace EmergencyResponse.Application.DTOs.Authentication;

public sealed record AuthenticationResponse
{
    public string AccessToken { get; init; } = null!;

    public DateTime ExpiresAtUtc { get; init; }

    public UserResponse User { get; init; } = null!;
}