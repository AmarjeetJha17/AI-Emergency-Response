using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using EmergencyResponse.Application.DTOs.Authentication;

namespace EmergencyResponse.IntegrationTests.Auth;

/// <summary>
/// Authentication endpoint tests against the dedicated SQL Server test database.
/// </summary>
[Trait(EmergencyResponse.Testing.TestCategories.TraitName, EmergencyResponse.Testing.TestCategories.Integration)]
[Collection("SQL Server integration database")]
public sealed class AuthEndpointTests
    : IClassFixture<EmergencyResponseWebApplicationFactory>
{
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthEndpointTests(EmergencyResponseWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Register ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_ValidRequest_Returns201()
    {
        var request = new RegisterRequest
        {
            FirstName = "Jane",
            LastName  = "Doe",
            Email     = $"jane.{Guid.NewGuid():N}@test.local",
            Password  = "Test@123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<UserResponse>(JsonOptions);

        Assert.NotNull(body);
        Assert.NotEmpty(body!.Id);
        Assert.Equal("Jane", body.FirstName);
        Assert.Equal("Doe",  body.LastName);
        Assert.Equal(request.Email, body.Email);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        var email = $"dup.{Guid.NewGuid():N}@test.local";

        var request = new RegisterRequest
        {
            FirstName = "A",
            LastName  = "B",
            Email     = email,
            Password  = "Test@123!"
        };

        await _client.PostAsJsonAsync("/api/auth/register", request);

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingEmail_Returns400()
    {
        var request = new
        {
            FirstName = "Jane",
            LastName  = "Doe",
            Email     = "",          // invalid
            Password  = "Test@123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400()
    {
        var request = new RegisterRequest
        {
            FirstName = "Jane",
            LastName  = "Doe",
            Email     = $"weak.{Guid.NewGuid():N}@test.local",
            Password  = "password"   // no uppercase, digit, or special char
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── Login ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var email    = $"login.{Guid.NewGuid():N}@test.local";
        var password = "Test@123!";

        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest
            {
                FirstName = "Login",
                LastName  = "User",
                Email     = email,
                Password  = password
            });

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = email, Password = password });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content
            .ReadFromJsonAsync<AuthenticationResponse>(JsonOptions);

        Assert.NotNull(body);
        Assert.NotEmpty(body!.AccessToken);
        Assert.Contains("Citizen", body.Roles);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var email = $"badpwd.{Guid.NewGuid():N}@test.local";

        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest
            {
                FirstName = "Bad",
                LastName  = "Password",
                Email     = email,
                Password  = "Test@123!"
            });

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = email, Password = "Wrong@999!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email    = "nobody@nowhere.test",
                Password = "Test@123!"
            });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidEmailFormat_Returns400()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email    = "not-an-email",
                Password = "Test@123!"
            });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
