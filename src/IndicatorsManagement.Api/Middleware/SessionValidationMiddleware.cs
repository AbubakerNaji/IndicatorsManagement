using System.Security.Claims;
using IndicatorsManagement.Infrastructure.Data;
using IndicatorsManagement.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Api.Middleware;

/// <summary>
/// Validates that the JWT token corresponds to an active, non-expired session.
/// Updates LastActivity on each request. Returns 401 if session expired or not found.
/// </summary>
public class SessionValidationMiddleware
{
    private readonly RequestDelegate _next;

    public SessionValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var token = context.Request.Headers.Authorization.ToString().Replace("Bearer ", "");
            if (!string.IsNullOrEmpty(token))
            {
                var db = context.RequestServices.GetRequiredService<IndicatorsDbContext>();
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId != null)
                {
                    // S2 — user_sessions stores SHA-256 hashes; hash the incoming JWT before lookup.
                    var hashed = SessionTokenHasher.Hash(token);
                    var session = await db.UserSessions
                        .FirstOrDefaultAsync(s => s.UserId == int.Parse(userId) && s.SessionToken == hashed);

                    if (session is null || session.ExpiresAt < DateTime.UtcNow)
                    {
                        // Session expired or not found — remove and reject
                        if (session is not null)
                        {
                            db.UserSessions.Remove(session);
                            await db.SaveChangesAsync();
                        }

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { message = "انتهت صلاحية الجلسة. يرجى تسجيل الدخول مجدداً" });
                        return;
                    }

                    // Check inactivity (30 minutes default)
                    var sessionTimeoutMinutes = 30;
                    var timeoutConfig = await db.SystemConfigurations
                        .Where(c => c.ConfigKey == "SessionTimeout_Minutes")
                        .Select(c => c.ConfigValue)
                        .FirstOrDefaultAsync();
                    if (timeoutConfig != null) int.TryParse(timeoutConfig, out sessionTimeoutMinutes);

                    if (session.LastActivity.AddMinutes(sessionTimeoutMinutes) < DateTime.UtcNow)
                    {
                        db.UserSessions.Remove(session);
                        await db.SaveChangesAsync();

                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        await context.Response.WriteAsJsonAsync(new { message = "تم إنهاء الجلسة بسبب عدم النشاط. يرجى تسجيل الدخول مجدداً" });
                        return;
                    }

                    // Update last activity
                    session.LastActivity = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                }
            }
        }

        await _next(context);
    }
}
