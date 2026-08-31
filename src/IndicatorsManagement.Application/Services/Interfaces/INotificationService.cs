using IndicatorsManagement.Contracts.Responses;
using IndicatorsManagement.Domain.Enums;

namespace IndicatorsManagement.Application.Services.Interfaces;

public interface INotificationService
{
    Task CreateNotificationAsync(int userId, NotificationType type, string titleAr, string messageAr, string? relatedEntityType = null, int? relatedEntityId = null);
    Task<ApiResponse<PaginatedResponse<NotificationResponse>>> GetNotificationsAsync(int userId, NotificationType? type = null, int page = 1, int pageSize = 20);
    Task<ApiResponse> MarkAsReadAsync(int notificationId, int userId);
    Task<ApiResponse> MarkAllAsReadAsync(int userId);
}
