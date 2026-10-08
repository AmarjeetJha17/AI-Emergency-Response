namespace EmergencyResponse.Api.Middleware;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["Referrer-Policy"] =
            "strict-origin-when-cross-origin";
        context.Response.Headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=()";

        await _next(context);
    }
}
