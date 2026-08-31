import api from "./api";
import type { ApiResponse } from "./authService";
import type { PaginatedResponse } from "./indicatorService";

export interface NotificationResponse {
  id: number;
  notificationType: string;
  titleAr: string;
  messageAr: string;
  relatedEntityType: string | null;
  relatedEntityId: number | null;
  isRead: boolean;
  sentAt: string;
  readAt: string | null;
}

export const notificationService = {
  getAll: (params?: { type?: string; page?: number; pageSize?: number }) =>
    api.get<ApiResponse<PaginatedResponse<NotificationResponse>>>("/notifications", { params }),

  markAsRead: (id: number) =>
    api.put<ApiResponse<null>>(`/notifications/${id}/read`),

  markAllAsRead: () =>
    api.put<ApiResponse<null>>("/notifications/read-all"),
};
