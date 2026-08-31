using IndicatorsManagement.Application.Services.Interfaces;
using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Entities;
using IndicatorsManagement.Domain.Enums;
using IndicatorsManagement.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace IndicatorsManagement.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly IndicatorsDbContext _db;

    public NotificationService(IndicatorsDbContext db)
    {
        _db = db;
    }

    public async Task CreateNotificationAsync(int userId, NotificationType type, string titleAr, string messageAr,
        string? relatedEntityType = null, int? relatedEntityId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            NotificationType = type,
            TitleAr = titleAr,
            MessageAr = messageAr,
            RelatedEntityType = relatedEntityType,
            RelatedEntityId = relatedEntityId,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();
    }

    public async Task<ApiResponse<PaginatedResponse<NotificationResponse>>> GetNotificationsAsync(
        int userId, NotificationType? type = null, int page = 1, int pageSize = 20)
    {
        var query = _db.Notifications.AsNoTracking()
            .Where(n => n.UserId == userId)
            .AsQueryable();

        if (type.HasValue)
            query = query.Where(n => n.NotificationType == type.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.SentAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(n => new NotificationResponse
            {
                Id = n.Id,
                NotificationType = n.NotificationType,
                TitleAr = n.TitleAr,
                MessageAr = n.MessageAr,
                RelatedEntityType = n.RelatedEntityType,
                RelatedEntityId = n.RelatedEntityId,
                IsRead = n.IsRead,
                SentAt = n.SentAt,
                ReadAt = n.ReadAt
            }).ToListAsync();

        return ApiResponse<PaginatedResponse<NotificationResponse>>.Ok(new PaginatedResponse<NotificationResponse>
        {
            Items = items, TotalCount = totalCount, Page = page, PageSize = pageSize
        });
    }

    public async Task<ApiResponse> MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification is null)
            return ApiResponse.Fail("الإشعار غير موجود");

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ApiResponse.Ok("تم تعليم الإشعار كمقروء");
    }

    public async Task<ApiResponse> MarkAllAsReadAsync(int userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return ApiResponse.Ok("تم تعليم جميع الإشعارات كمقروءة");
    }
}
