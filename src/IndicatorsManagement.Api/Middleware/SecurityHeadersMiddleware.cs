namespace IndicatorsManagement.Api.Middleware;

/// <summary>
/// S9 — adds baseline HTTP security response headers. Prevents clickjacking, MIME
/// sniffing, and leaks of Referer; installs a Content-Security-Policy tuned for a
/// Vite/React SPA that talks to this same origin.
/// </summary>
public class SecurityHeadersMiddleware
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
        headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), payment=()";
        // Keep CSP conservative but usable for the bundled SPA and its API calls.
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "img-src 'self' data: blob:; " +
            "style-src 'self' 'unsafe-inline'; " +   // TailAdmin ships inline styles.
            "script-src 'self'; " +
            "connect-src 'self'; " +
            "font-src 'self' data:; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'";

        return _next(context);
    }
}
