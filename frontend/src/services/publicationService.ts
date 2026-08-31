import api from "./api";
import type { ApiResponse } from "./authService";

export interface PublicationHistoryItem {
  id: number;
  indicatorEntryId: number;
  action: string;
  performedByName: string;
  performedAt: string;
  reason: string | null;
}

export interface PublicationStatistics {
  totalPublished: number;
  totalUnpublished: number;
  totalFinalApproved: number;
  publicationRate: number;
  latestPublicationDate: string | null;
}

export const publicationService = {
  publish: (entryId: number, reason?: string) =>
    api.post<ApiResponse<null>>(`/indicator-entries/${entryId}/publish`, { reason }),

  unpublish: (entryId: number, reason?: string) =>
    api.post<ApiResponse<null>>(`/indicator-entries/${entryId}/unpublish`, { reason }),

  bulkPublish: (entryIds: number[], reason?: string) =>
    api.post<ApiResponse<number>>("/indicator-entries/bulk-publish", { entryIds, reason }),

  bulkUnpublish: (entryIds: number[], reason?: string) =>
    api.post<ApiResponse<number>>("/indicator-entries/bulk-unpublish", { entryIds, reason }),

  getHistory: (entryId: number) =>
    api.get<ApiResponse<PublicationHistoryItem[]>>(`/indicator-entries/${entryId}/publication-history`),

  getStatistics: () =>
    api.get<ApiResponse<PublicationStatistics>>("/publication/statistics"),
};
