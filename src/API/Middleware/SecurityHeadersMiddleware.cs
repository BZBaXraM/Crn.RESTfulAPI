namespace API.Middleware;

/// <summary>Adds defensive HTTP response headers to every response.</summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";

        // Swagger UI needs inline scripts/styles; the API itself serves only JSON.
        if (!context.Request.Path.StartsWithSegments("/swagger"))
        {
            headers["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";
        }

        return _next(context);
    }
}
