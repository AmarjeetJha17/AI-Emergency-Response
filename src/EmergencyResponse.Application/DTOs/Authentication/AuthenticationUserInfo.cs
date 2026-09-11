namespace EmergencyResponse.Application.DTOs.Authentication;

public sealed record AuthenticationUserInfo
{
    public string Id { get; init; } = null!;

    public string Email { get; init; } = null!;

    public string FirstName { get; init; } = null!;

    public string LastName { get; init; } = null!;

    public IReadOnlyCollection<string> Roles { get; init; }
        = Array.Empty<string>();
}