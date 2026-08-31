using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class DraftRecoveryService : IDraftRecoveryService
{
    private readonly IndicatorsDbContext _db;

    public DraftRecoveryService(IndicatorsDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResponse> SaveDraftAsync(int userId, int indicatorId, int entityId, int reportingPeriodId, string draftDataJson)
    {
        var existing = await _db.DraftRecoveries
            .FirstOrDefaultAsync(d => d.UserId == userId && d.IndicatorId == indicatorId
                && d.EntityId == entityId && d.ReportingPeriodId == reportingPeriodId);

        if (existing is not null)
        {
            existing.DraftDataJson = draftDataJson;
            existing.LastSavedAt = DateTime.UtcNow;
            existing.ExpiresAt = DateTime.UtcNow.AddDays(7);
        }
        else
        {
            _db.DraftRecoveries.Add(new DraftRecovery
            {
                UserId = userId,
                IndicatorId = indicatorId,
                EntityId = entityId,
                ReportingPeriodId = reportingPeriodId,
                DraftDataJson = draftDataJson,
                LastSavedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });
        }

        await _db.SaveChangesAsync();
        return ApiResponse.Ok("تم حفظ المسودة");
    }

    public async Task<ApiResponse<List<DraftRecoveryResponse>>> GetRecoverableDraftsAsync(int userId)
    {
        var drafts = await _db.DraftRecoveries.AsNoTracking()
            .Include(d => d.Indicator)
            .Include(d => d.ReportingPeriod)
            .Where(d => d.UserId == userId && d.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(d => d.LastSavedAt)
            .Select(d => new DraftRecoveryResponse
            {
                Id = d.Id,
                IndicatorId = d.IndicatorId,
                IndicatorNameAr = d.Indicator.NameAr,
                EntityId = d.EntityId,
                ReportingPeriodId = d.ReportingPeriodId,
                PeriodDisplayNameAr = d.ReportingPeriod.DisplayNameAr,
                DraftDataJson = d.DraftDataJson,
                LastSavedAt = d.LastSavedAt
            }).ToListAsync();

        return ApiResponse<List<DraftRecoveryResponse>>.Ok(drafts);
    }

    public async Task<ApiResponse> DiscardDraftAsync(int draftId, int userId)
    {
        var draft = await _db.DraftRecoveries
            .FirstOrDefaultAsync(d => d.Id == draftId && d.UserId == userId);

        if (draft is null)
            return ApiResponse.Fail("المسودة غير موجودة");

        _db.DraftRecoveries.Remove(draft);
        await _db.SaveChangesAsync();
        return ApiResponse.Ok("تم حذف المسودة");
    }
}
