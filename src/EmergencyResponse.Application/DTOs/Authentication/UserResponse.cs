namespace EmergencyResponse.Application.DTOs.Authentication;

public sealed record UserResponse
{
    public string Id { get; init; } = null!;

    public string FirstName { get; init; } = null!;

    public string LastName { get; init; } = null!;

    public string Email { get; init; } = null!;

    public string? PhoneNumber { get; init; }

    public bool IsActive { get; init; }
}