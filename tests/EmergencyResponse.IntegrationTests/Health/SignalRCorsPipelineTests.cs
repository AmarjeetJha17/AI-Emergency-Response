using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using EmergencyResponse.Api.Configuration;
using EmergencyResponse.Api.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace EmergencyResponse.IntegrationTests.Health;

// An isolated HTTP pipeline: no application startup, database, workers or external AI.
public sealed class SignalRCorsPipelineTests
{
    private const string Origin = "https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io";
    private const string SigningKey = "local-test-only-signing-key-at-least-32-bytes";

    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("invalid", HttpStatusCode.Unauthorized)]
    [InlineData("valid", HttpStatusCode.OK)]
    public async Task Negotiate_RequiresValidBearerToken_EvenForTrustedOrigin(string? token, HttpStatusCode expected)
    {
        await using var app = await StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/hubs/incidents/negotiate?negotiateVersion=1");
        request.Headers.Add("Origin", Origin);
        if (token is not null)
            request.Headers.Authorization = new("Bearer", token == "valid" ? CreateToken() : "invalid");
        using var response = await client.SendAsync(request);
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal(Origin, Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
        if (expected == HttpStatusCode.Unauthorized) Assert.Contains(response.Headers.WwwAuthenticate, x => x.Scheme == "Bearer");
    }

    [Theory]
    [InlineData(Origin, true)]
    [InlineData("https://untrusted.example", false)]
    public async Task Preflight_GrantsOnlyTrustedOrigin_WithoutCredentials(string origin, bool allowed)
    {
        await using var app = await StartAsync();
        using var client = app.GetTestClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/hubs/incidents/negotiate?negotiateVersion=1");
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "authorization,content-type,x-requested-with,x-signalr-user-agent");
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Origin"));
        Assert.Equal(allowed, response.Headers.Contains("Access-Control-Allow-Credentials"));
    }

    private static async Task<WebApplication> StartAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Test" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Cors:AllowedOrigins:0"] = Origin });
        builder.Services.AddWebClientCors(builder.Configuration, builder.Environment);
        builder.Services.AddSignalR();
        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)),
                ValidateIssuer = true, ValidIssuer = "local-tests", ValidateAudience = true, ValidAudience = "local-tests",
                ValidateLifetime = true, ClockSkew = TimeSpan.Zero
            });
        builder.Services.AddAuthorization();
        var app = builder.Build();
        app.UseRouting();
        app.UseCors(WebClientCors.PolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapHub<IncidentHub>("/hubs/incidents");
        await app.StartAsync();
        return app;
    }

    private static string CreateToken() => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        issuer: "local-tests", audience: "local-tests", claims: [new Claim(ClaimTypes.NameIdentifier, "synthetic-test-user")],
        expires: DateTime.UtcNow.AddMinutes(5),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SigningKey)), SecurityAlgorithms.HmacSha256)));
}
