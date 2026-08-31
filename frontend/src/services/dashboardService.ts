import api from "./api";
import type { ApiResponse } from "./authService";

export interface DraftRecoveryItem {
  id: number;
  indicatorId: number;
  indicatorNameAr: string;
  entityId: number;
  reportingPeriodId: number;
  periodDisplayNameAr: string;
  draftDataJson: string;
  lastSavedAt: string;
}

export const draftService = {
  save: (data: { indicatorId: number; reportingPeriodId: number; draftDataJson: string }) =>
    api.post<ApiResponse<null>>("/drafts/save", data),

  getRecoverable: () =>
    api.get<ApiResponse<DraftRecoveryItem[]>>("/drafts/recover"),

  discard: (id: number) =>
    api.delete<ApiResponse<null>>(`/drafts/${id}`),
};
