import { useState, useCallback } from "react";

export interface PaginationState {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export function usePagination(defaultPageSize = 10) {
  const [pagination, setPagination] = useState<PaginationState>({
    page: 1,
    pageSize: defaultPageSize,
    totalCount: 0,
    totalPages: 0,
  });

  const setPage = useCallback((page: number) => {
    setPagination((prev) => ({ ...prev, page }));
  }, []);

  const updateFromResponse = useCallback((totalCount: number, page: number, pageSize: number) => {
    setPagination({
      page,
      pageSize,
      totalCount,
      totalPages: Math.ceil(totalCount / pageSize),
    });
  }, []);

  return { pagination, setPage, updateFromResponse };
}
