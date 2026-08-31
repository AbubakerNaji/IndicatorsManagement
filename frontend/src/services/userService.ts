import api from "./api";
import type { ApiResponse, UserInfo } from "./authService";
import type { PaginatedResponse } from "./indicatorService";

export interface CreateUserRequest {
  userName: string;
  email: string;
  fullNameAr: string;
  phone?: string;
  password: string;
  role: string;
  entityId?: number;
}

export interface UpdateUserRequest {
  fullNameAr: string;
  phone?: string;
  email: string;
  role: string;
  entityId?: number;
}

export const userService = {
  getAll: (params?: { entityId?: number; page?: number; pageSize?: number }) =>
    api.get<ApiResponse<PaginatedResponse<UserInfo>>>("/users", { params }),

  getById: (id: number) =>
    api.get<ApiResponse<UserInfo>>(`/users/${id}`),

  create: (data: CreateUserRequest) =>
    api.post<ApiResponse<UserInfo>>("/users", data),

  update: (id: number, data: UpdateUserRequest) =>
    api.put<ApiResponse<UserInfo>>(`/users/${id}`, data),

  deactivate: (id: number) =>
    api.delete<ApiResponse<null>>(`/users/${id}`),
};
