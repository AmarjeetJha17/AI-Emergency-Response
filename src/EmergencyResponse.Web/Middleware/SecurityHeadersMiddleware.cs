namespace EmergencyResponse.Web.Middleware;

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
            "camera=(), microphone=(), geolocation=(self)";
        context.Response.Headers["Content-Security-Policy-Report-Only"] =
            "default-src 'self'; " +
            "base-uri 'self'; " +
            "frame-ancestors 'self'; " +
            "form-action 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
            "img-src 'self' data: blob: https:; " +
            "font-src 'self' data: https://cdn.jsdelivr.net; " +
            "connect-src 'self' https: wss:;";

        await _next(context);
    }
}
