namespace EmergencyResponse.Infrastructure.Identity;

public sealed class AdminSeedOptions
{
    public const string SectionName = "AdminSeed";

    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;
}