using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job: removes expired sessions hourly.
/// </summary>
public class SessionCleanupJob
{
    private readonly IndicatorsDbContext _db;

    public SessionCleanupJob(IndicatorsDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync()
    {
        var now = DateTime.UtcNow;

        var expiredSessions = await _db.UserSessions
            .Where(s => s.ExpiresAt < now)
            .ToListAsync();

        if (expiredSessions.Count > 0)
        {
            _db.UserSessions.RemoveRange(expiredSessions);
            await _db.SaveChangesAsync();
        }
    }
}
