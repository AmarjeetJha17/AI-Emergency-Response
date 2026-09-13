using System.Net.Http.Headers;
using System.Security.Claims;

using EmergencyResponse.Application.Common.Authentication;

using Microsoft.AspNetCore.Authentication;

namespace EmergencyResponse.Web.Services.Api;

public sealed class BearerTokenHandler
    : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<BearerTokenHandler> _logger;

    public BearerTokenHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<BearerTokenHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext =
            _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return await base.SendAsync(
                request,
                cancellationToken);
        }

        var accessToken =
            httpContext.User.FindFirstValue(
                AuthenticationConstants.AccessTokenClaim);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);
        }
        else
        {
            _logger.LogDebug(
                "No API access token found for outgoing request {Method} {Uri}.",
                request.Method,
                request.RequestUri);
        }

        return await base.SendAsync(
            request,
            cancellationToken);
    }
}