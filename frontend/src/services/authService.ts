import api from "./api";

export interface LoginRequest {
  userNameOrEmail: string;
  password: string;
}

export interface UserInfo {
  id: number;
  userName: string;
  fullNameAr: string;
  email: string;
  role: string;
  entityId: number | null;
  entityNameAr: string | null;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: UserInfo;
}

export interface ApiResponse<T> {
  success: boolean;
  message: string | null;
  data: T | null;
  errors: string[] | null;
}

export const authService = {
  login: (data: LoginRequest) =>
    api.post<ApiResponse<LoginResponse>>("/auth/login", data),

  logout: () => api.post<ApiResponse<null>>("/auth/logout"),

  adminResetPassword: (userId: number, newPassword: string) =>
    api.post<ApiResponse<null>>("/auth/admin-reset-password", {
      userId,
      newPassword,
    }),
};
