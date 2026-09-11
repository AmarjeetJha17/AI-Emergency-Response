namespace EmergencyResponse.Application.DTOs.Authentication;

public sealed record RegisterRequest
{
    public string FirstName { get; init; } = null!;

    public string LastName { get; init; } = null!;

    public string Email { get; init; } = null!;

    public string Password { get; init; } = null!;

    public string? PhoneNumber { get; init; }
}