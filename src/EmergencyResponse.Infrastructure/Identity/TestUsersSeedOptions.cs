namespace EmergencyResponse.Infrastructure.Identity;

/// <summary>
/// Configuration for a single test/development seed user.
/// Bind from appsettings.Development.json under TestUsers:{name}.
/// Never put passwords in source-controlled appsettings.json.
/// </summary>
public sealed class TestUserSeedEntry
{
    public string Email { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Role { get; set; } = null!;
}

public sealed class TestUsersSeedOptions
{
    public const string SectionName = "TestUsersSeed";

    public TestUserSeedEntry Operator { get; set; } = new();

    public TestUserSeedEntry EmergencyAdmin { get; set; } = new();
}
