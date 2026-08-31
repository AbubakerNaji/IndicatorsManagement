using System.Security.Claims;
using IndicatorsManagement.Application.Services.Interfaces;

namespace IndicatorsManagement.Api.Middleware;

/// <summary>
/// Automatically logs API requests for write operations (POST, PUT, DELETE) to the audit log.
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly HashSet<string> AuditableMethods = ["POST", "PUT", "DELETE", "PATCH"];

    public AuditLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Only audit write operations from authenticated users
        if (!AuditableMethods.Contains(context.Request.Method))
            return;

        if (context.User.Identity?.IsAuthenticated != true)
            return;

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return;

        var audit = context.RequestServices.GetService<IAuditLogService>();
        if (audit is null) return;

        var path = context.Request.Path.Value ?? "";
        var statusCode = context.Response.StatusCode;
        var resultStatus = statusCode >= 200 && statusCode < 300 ? "Success" : "Failure";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();

        await audit.LogAsync(
            int.Parse(userId),
            "API",
            null,
            $"{context.Request.Method} {path}",
            ipAddress: ipAddress,
            resultStatus: resultStatus,
            errorCode: statusCode >= 400 ? statusCode.ToString() : null);
    }
}
