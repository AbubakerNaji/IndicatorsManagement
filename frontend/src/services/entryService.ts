import api from "./api";
import type { ApiResponse } from "./authService";
import type { PaginatedResponse } from "./indicatorService";

export interface IndicatorEntryResponse {
  id: number;
  indicatorId: number;
  indicatorNameAr: string;
  indicatorCode: string;
  entityId: number;
  entityNameAr: string;
  reportingPeriodId: number;
  periodDisplayNameAr: string;
  valueNumeric: number | null;
  valueText: string | null;
  unitSnapshot: string | null;
  workflowState: string;
  versionNo: number;
  notes: string | null;
  source: string | null;
  rejectionReason: string | null;
  enteredAt: string;
  enteredByName: string;
  submittedAt: string | null;
  dimensions: EntryDimensionResponse[];
  attachments: AttachmentResponse[];
}

export interface EntryDimensionResponse {
  id: number;
  dimensionId: number;
  dimensionNameAr: string;
  dimensionValueId: number | null;
  dimensionValueAr: string | null;
  valueNumeric: number | null;
}

export interface AttachmentResponse {
  id: number;
  fileName: string;
  fileType: string;
  fileSize: number;
  description: string | null;
  uploadedAt: string;
}

export const entryService = {
  getAll: (params?: { entityId?: number; indicatorId?: number; periodId?: number; page?: number; pageSize?: number }) =>
    api.get<ApiResponse<PaginatedResponse<IndicatorEntryResponse>>>("/indicator-entries", { params }),

  getById: (id: number) =>
    api.get<ApiResponse<IndicatorEntryResponse>>(`/indicator-entries/${id}`),

  create: (data: Record<string, unknown>) =>
    api.post<ApiResponse<IndicatorEntryResponse>>("/indicator-entries", data),

  update: (id: number, data: Record<string, unknown>) =>
    api.put<ApiResponse<IndicatorEntryResponse>>(`/indicator-entries/${id}`, data),

  delete: (id: number) =>
    api.delete<ApiResponse<null>>(`/indicator-entries/${id}`),

  submit: (id: number) =>
    api.post<ApiResponse<IndicatorEntryResponse>>(`/indicator-entries/${id}/submit`),

  approveEntity: (id: number, notes?: string) =>
    api.post<ApiResponse<IndicatorEntryResponse>>(`/indicator-entries/${id}/approve-entity`, { notes }),

  approveMinistry: (id: number, notes?: string) =>
    api.post<ApiResponse<IndicatorEntryResponse>>(`/indicator-entries/${id}/approve-ministry`, { notes }),

  reject: (id: number, notes: string) =>
    api.post<ApiResponse<IndicatorEntryResponse>>(`/indicator-entries/${id}/reject`, { notes }),

  returnEntry: (id: number, notes: string) =>
    api.post<ApiResponse<IndicatorEntryResponse>>(`/indicator-entries/${id}/return`, { notes }),
};
