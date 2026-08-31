using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Jobs;

/// <summary>
/// Hangfire recurring job: removes expired draft recovery records weekly.
/// </summary>
public class DraftCleanupJob
{
    private readonly IndicatorsDbContext _db;

    public DraftCleanupJob(IndicatorsDbContext db)
    {
        _db = db;
    }

    public async Task ExecuteAsync()
    {
        var expired = await _db.DraftRecoveries
            .Where(d => d.ExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        if (expired.Count > 0)
        {
            _db.DraftRecoveries.RemoveRange(expired);
            await _db.SaveChangesAsync();
        }
    }
}
