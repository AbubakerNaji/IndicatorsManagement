import api from "./api";
import type { ApiResponse } from "./authService";

export interface IndicatorResponse {
  id: number;
  code: string;
  nameAr: string;
  nameEn: string | null;
  definitionAr: string;
  calculationMethodAr: string;
  unitAr: string;
  dataSourceAr: string;
  objectiveAr: string | null;
  publicationFrequency: string;
  isActive: boolean;
  requiresAttachment: boolean;
  requiresReview: boolean;
  dimensions: DimensionResponse[];
}

export interface DimensionResponse {
  id: number;
  dimensionNameAr: string;
  dimensionType: string;
  isMandatory: boolean;
  displayOrder: number;
  values: DimensionValueResponse[];
}

export interface DimensionValueResponse {
  id: number;
  valueAr: string;
  valueEn: string | null;
  displayOrder: number;
  isActive: boolean;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export const indicatorService = {
  getAll: (params?: { isActive?: boolean; page?: number; pageSize?: number }) =>
    api.get<ApiResponse<PaginatedResponse<IndicatorResponse>>>("/indicators", { params }),

  getById: (id: number) =>
    api.get<ApiResponse<IndicatorResponse>>(`/indicators/${id}`),

  create: (data: Record<string, unknown>) =>
    api.post<ApiResponse<IndicatorResponse>>("/indicators", data),

  update: (id: number, data: Record<string, unknown>) =>
    api.put<ApiResponse<IndicatorResponse>>(`/indicators/${id}`, data),

  delete: (id: number) =>
    api.delete<ApiResponse<null>>(`/indicators/${id}`),
};
