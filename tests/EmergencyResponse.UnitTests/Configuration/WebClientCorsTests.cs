using EmergencyResponse.Api.Configuration;
using EmergencyResponse.Api.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EmergencyResponse.UnitTests.Configuration;

public sealed class WebClientCorsTests
{
    private const string WebOrigin = "https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io";

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task TrustedOrigin_PreflightSupportsSignalR_WithoutReachingAuthentication(string method)
    {
        using var services = CreateServices(WebOrigin);
        var context = Preflight(WebOrigin, method);
        var reachedNext = false;
        var middleware = new CorsMiddleware(_ =>
        {
            reachedNext = true;
            throw new InvalidOperationException("Preflight must finish before authentication.");
        }, services.GetRequiredService<ICorsService>(), NullLoggerFactory.Instance, WebClientCors.PolicyName);

        await middleware.Invoke(context, services.GetRequiredService<ICorsPolicyProvider>());

        Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode);
        Assert.False(reachedNext);
        Assert.Equal(WebOrigin, context.Response.Headers.AccessControlAllowOrigin.ToString());
        Assert.Equal("true", context.Response.Headers.AccessControlAllowCredentials.ToString());
        Assert.Contains(method, context.Response.Headers.AccessControlAllowMethods.ToString());
        foreach (var header in new[] { "authorization", "content-type", "x-requested-with", "x-signalr-user-agent" })
            Assert.Contains(header, context.Response.Headers.AccessControlAllowHeaders.ToString());
    }

    [Theory]
    [InlineData("https://untrusted.example")]
    [InlineData("https://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io.attacker.example")]
    [InlineData("http://ca-er-web-portfolio.blacksand-fa4db74f.centralus.azurecontainerapps.io")]
    [InlineData("null")]
    public async Task UntrustedOrigin_PreflightDoesNotGrantBrowserAccess(string origin)
    {
        using var services = CreateServices(WebOrigin);
        var context = Preflight(origin, "POST");
        await InvokeCors(services, context);
        Assert.False(context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
        Assert.False(context.Response.Headers.ContainsKey("Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task MissingStagingConfiguration_DoesNotAllowPortfolioOrLocalhost()
    {
        using var services = CreateServices(null);
        foreach (var origin in new[] { WebOrigin, "http://localhost:5021" })
        {
            var context = Preflight(origin, "POST");
            await InvokeCors(services, context);
            Assert.False(context.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
        }
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.example.com")]
    [InlineData(WebOrigin + "/")]
    [InlineData(WebOrigin + "/path")]
    [InlineData(WebOrigin + "?query=true")]
    [InlineData("https://user:password@example.com")]
    public void InvalidOriginConfiguration_IsRejected(string origin)
    {
        Assert.Throws<InvalidOperationException>(() => CreateServices(origin));
    }

    [Fact]
    public void HubStillRequiresAuthentication()
    {
        Assert.NotEmpty(typeof(IncidentHub).GetCustomAttributes(typeof(AuthorizeAttribute), true));
        Assert.Empty(typeof(IncidentHub).GetCustomAttributes(typeof(AllowAnonymousAttribute), true));
    }

    private static ServiceProvider CreateServices(string? origin)
    {
        var values = new Dictionary<string, string?>();
        if (origin is not null) values["Cors:AllowedOrigins:0"] = origin;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(x => x.EnvironmentName).Returns("Staging");
        return new ServiceCollection().AddLogging()
            .AddWebClientCors(configuration, environment.Object).BuildServiceProvider();
    }

    private static DefaultHttpContext Preflight(string origin, string method)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "OPTIONS";
        context.Request.Scheme = "https";
        context.Request.Path = "/hubs/incidents/negotiate";
        context.Request.Headers.Origin = origin;
        context.Request.Headers.AccessControlRequestMethod = method;
        context.Request.Headers.AccessControlRequestHeaders = "authorization,content-type,x-requested-with,x-signalr-user-agent";
        return context;
    }

    private static Task InvokeCors(ServiceProvider services, HttpContext context) =>
        new CorsMiddleware(_ => Task.CompletedTask, services.GetRequiredService<ICorsService>(),
            NullLoggerFactory.Instance, WebClientCors.PolicyName)
            .Invoke(context, services.GetRequiredService<ICorsPolicyProvider>());
}
