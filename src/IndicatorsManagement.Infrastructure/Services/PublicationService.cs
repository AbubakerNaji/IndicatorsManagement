using System.Text.Json;
using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class PublicationService : IPublicationService
{
    private readonly IndicatorsDbContext _db;
    private readonly IAuditLogService _audit;
    private readonly INotificationService _notification;

    public PublicationService(IndicatorsDbContext db, IAuditLogService audit, INotificationService notification)
    {
        _db = db;
        _audit = audit;
        _notification = notification;
    }

    public async Task<ApiResponse> PublishEntryAsync(int entryId, int userId, string? reason)
    {
        var entry = await _db.IndicatorEntries.FirstOrDefaultAsync(e => e.Id == entryId && !e.IsDeleted);
        if (entry is null) return ApiResponse.Fail("الإدخال غير موجود");
        if (entry.WorkflowState != WorkflowState.Final_Approved)
            return ApiResponse.Fail("لا يمكن نشر إدخال غير معتمد نهائياً");
        if (entry.PublicationStatus == PublicationStatus.Published)
            return ApiResponse.Fail("الإدخال منشور مسبقاً");

        entry.PublicationStatus = PublicationStatus.Published;
        entry.UpdatedAt = DateTime.UtcNow;

        _db.PublicationHistories.Add(new PublicationHistory
        {
            IndicatorEntryId = entryId, Action = "Published", PerformedBy = userId, Reason = reason
        });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "IndicatorEntry", entryId, "Publish",
            newValues: JsonSerializer.Serialize(new { Status = "Published", reason }));

        // Notify Viewer users
        await NotifyViewersAsync($"تم نشر بيانات جديدة", "IndicatorEntry", entryId);

        return ApiResponse.Ok("تم نشر الإدخال بنجاح");
    }

    public async Task<ApiResponse> UnpublishEntryAsync(int entryId, int userId, string? reason)
    {
        var entry = await _db.IndicatorEntries.FirstOrDefaultAsync(e => e.Id == entryId && !e.IsDeleted);
        if (entry is null) return ApiResponse.Fail("الإدخال غير موجود");
        if (entry.PublicationStatus != PublicationStatus.Published)
            return ApiResponse.Fail("الإدخال غير منشور");

        entry.PublicationStatus = PublicationStatus.Unpublished;
        entry.UpdatedAt = DateTime.UtcNow;

        _db.PublicationHistories.Add(new PublicationHistory
        {
            IndicatorEntryId = entryId, Action = "Unpublished", PerformedBy = userId, Reason = reason
        });

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "IndicatorEntry", entryId, "Unpublish",
            newValues: JsonSerializer.Serialize(new { Status = "Unpublished", reason }));

        return ApiResponse.Ok("تم إلغاء نشر الإدخال بنجاح");
    }

    public async Task<ApiResponse<int>> BulkPublishAsync(List<int> entryIds, int userId, string? reason)
    {
        var entries = await _db.IndicatorEntries
            .Where(e => entryIds.Contains(e.Id) && !e.IsDeleted
                && e.WorkflowState == WorkflowState.Final_Approved
                && e.PublicationStatus == PublicationStatus.Unpublished)
            .ToListAsync();

        if (entries.Count == 0)
            return ApiResponse<int>.Fail("لا توجد إدخالات صالحة للنشر");

        foreach (var entry in entries)
        {
            entry.PublicationStatus = PublicationStatus.Published;
            entry.UpdatedAt = DateTime.UtcNow;
            _db.PublicationHistories.Add(new PublicationHistory
            {
                IndicatorEntryId = entry.Id, Action = "Published", PerformedBy = userId, Reason = reason ?? "نشر جماعي"
            });
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "IndicatorEntry", null, "Bulk_Publish",
            newValues: $"Published {entries.Count} entries");

        await NotifyViewersAsync($"تم نشر {entries.Count} إدخال جديد", null, null);

        return ApiResponse<int>.Ok(entries.Count, $"تم نشر {entries.Count} إدخال بنجاح");
    }

    public async Task<ApiResponse<int>> BulkUnpublishAsync(List<int> entryIds, int userId, string? reason)
    {
        var entries = await _db.IndicatorEntries
            .Where(e => entryIds.Contains(e.Id) && !e.IsDeleted && e.PublicationStatus == PublicationStatus.Published)
            .ToListAsync();

        if (entries.Count == 0)
            return ApiResponse<int>.Fail("لا توجد إدخالات منشورة للإلغاء");

        foreach (var entry in entries)
        {
            entry.PublicationStatus = PublicationStatus.Unpublished;
            entry.UpdatedAt = DateTime.UtcNow;
            _db.PublicationHistories.Add(new PublicationHistory
            {
                IndicatorEntryId = entry.Id, Action = "Unpublished", PerformedBy = userId, Reason = reason ?? "إلغاء نشر جماعي"
            });
        }

        await _db.SaveChangesAsync();
        await _audit.LogAsync(userId, "IndicatorEntry", null, "Bulk_Unpublish",
            newValues: $"Unpublished {entries.Count} entries");

        return ApiResponse<int>.Ok(entries.Count, $"تم إلغاء نشر {entries.Count} إدخال بنجاح");
    }

    public async Task<ApiResponse<List<PublicationHistoryResponse>>> GetPublicationHistoryAsync(int entryId)
    {
        var history = await _db.PublicationHistories.AsNoTracking()
            .Include(h => h.PerformedByUser)
            .Where(h => h.IndicatorEntryId == entryId)
            .OrderByDescending(h => h.PerformedAt)
            .Select(h => new PublicationHistoryResponse
            {
                Id = h.Id,
                IndicatorEntryId = h.IndicatorEntryId,
                Action = h.Action,
                PerformedByName = h.PerformedByUser.FullNameAr,
                PerformedAt = h.PerformedAt,
                Reason = h.Reason
            }).ToListAsync();

        return ApiResponse<List<PublicationHistoryResponse>>.Ok(history);
    }

    public async Task<ApiResponse<PublicationStatisticsResponse>> GetPublicationStatisticsAsync()
    {
        var published = await _db.IndicatorEntries.CountAsync(e => !e.IsDeleted && e.PublicationStatus == PublicationStatus.Published);
        var unpublished = await _db.IndicatorEntries.CountAsync(e => !e.IsDeleted && e.WorkflowState == WorkflowState.Final_Approved && e.PublicationStatus == PublicationStatus.Unpublished);
        var totalApproved = published + unpublished;
        var latestDate = await _db.PublicationHistories.Where(h => h.Action == "Published").MaxAsync(h => (DateTime?)h.PerformedAt);

        return ApiResponse<PublicationStatisticsResponse>.Ok(new PublicationStatisticsResponse
        {
            TotalPublished = published,
            TotalUnpublished = unpublished,
            TotalFinalApproved = totalApproved,
            PublicationRate = totalApproved > 0 ? Math.Round((decimal)published / totalApproved * 100, 1) : 0,
            LatestPublicationDate = latestDate
        });
    }

    private async Task NotifyViewersAsync(string message, string? relatedType, int? relatedId)
    {
        var viewerIds = await _db.UserRoles
            .Join(_db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Where(x => x.Name == "Viewer")
            .Select(x => x.UserId)
            .ToListAsync();

        foreach (var uid in viewerIds)
            await _notification.CreateNotificationAsync(uid, NotificationType.Publication, "نشر بيانات جديدة", message, relatedType, relatedId);
    }
}
