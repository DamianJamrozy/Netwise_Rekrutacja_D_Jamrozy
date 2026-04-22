namespace Netwise_Rekrutacja_D_Jamrozy.Infrastructure.Security;

public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Permitted-Cross-Domain-Policies"] = "none";
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self'; " +
            "img-src 'self' data:; " +
            "font-src 'self' data:; " +
            "connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self';";

        await next(context);
    }
}