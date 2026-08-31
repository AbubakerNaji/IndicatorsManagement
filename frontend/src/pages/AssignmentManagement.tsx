import { useEffect, useState } from "react";
import PageMeta from "../components/common/PageMeta";
import PageBreadCrumb from "../components/common/PageBreadCrumb";
import api from "../services/api";
import type { ApiResponse } from "../services/authService";
import type { PaginatedResponse } from "../services/indicatorService";
import Button from "../components/ui/button/Button";
import Pagination from "../components/ui/pagination/Pagination";
import { usePagination } from "../hooks/usePagination";

interface AssignmentResponse {
  id: number;
  indicatorId: number;
  indicatorNameAr: string;
  indicatorCode: string;
  entityId: number;
  entityNameAr: string;
  startDate: string;
  endDate: string | null;
  reportingFrequency: string;
  isActive: boolean;
}

const FREQ_LABELS: Record<string, string> = {
  Monthly: "شهري", Quarterly: "ربع سنوي", Semi_Annual: "نصف سنوي", Annual: "سنوي",
};

export default function AssignmentManagement() {
  const [assignments, setAssignments] = useState<AssignmentResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const { pagination, setPage, updateFromResponse } = usePagination(10);

  const loadAssignments = async (page = pagination.page) => {
    setLoading(true);
    try {
      const res = await api.get<ApiResponse<PaginatedResponse<AssignmentResponse>>>("/indicator-assignments", { params: { pageSize: 10, page } });
      const data = res.data.data;
      setAssignments(data?.items || []);
      if (data) updateFromResponse(data.totalCount, data.page, data.pageSize);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadAssignments(); }, [pagination.page]);

  const handleGenerateObligations = async (id: number) => {
    const res = await api.post<ApiResponse<number>>(`/indicator-assignments/${id}/generate-obligations`);
    alert(res.data.message || "تم إنشاء الالتزامات");
  };

  return (
    <>
      <PageMeta title="إدارة التكليفات | نظام إدارة المؤشرات" />
      <PageBreadCrumb pageTitle="إدارة التكليفات" />

      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03] lg:p-6">
        <div className="flex items-center justify-between mb-5">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90">التكليفات</h3>
          <Button size="sm">إضافة تكليف</Button>
        </div>

        {loading ? (
          <p className="text-gray-500 text-center py-8">جارٍ التحميل...</p>
        ) : (
          <>
          <div className="overflow-x-auto">
            <table className="w-full table-auto">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700">
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">المؤشر</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الجهة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الدورية</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">تاريخ البداية</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">تاريخ النهاية</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الحالة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">إجراءات</th>
                </tr>
              </thead>
              <tbody>
                {assignments.map((a) => (
                  <tr key={a.id} className="border-b border-gray-100 dark:border-gray-800">
                    <td className="px-4 py-3 text-sm">
                      <div className="text-gray-800 dark:text-white/90">{a.indicatorNameAr}</div>
                      <div className="text-xs text-gray-400 font-mono">{a.indicatorCode}</div>
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{a.entityNameAr}</td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{FREQ_LABELS[a.reportingFrequency] || a.reportingFrequency}</td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{a.startDate}</td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{a.endDate || "—"}</td>
                    <td className="px-4 py-3 text-sm">
                      <span className={`px-2 py-1 text-xs rounded-full ${a.isActive ? "bg-success-50 text-success-600" : "bg-gray-100 text-gray-500"}`}>
                        {a.isActive ? "فعّال" : "غير فعّال"}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-sm">
                      <button onClick={() => handleGenerateObligations(a.id)} className="text-brand-500 hover:text-brand-700 text-xs">
                        إنشاء التزامات
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination
            currentPage={pagination.page}
            totalPages={pagination.totalPages}
            totalCount={pagination.totalCount}
            pageSize={pagination.pageSize}
            onPageChange={(p) => { setPage(p); loadAssignments(p); }}
          />
          </>
        )}
      </div>
    </>
  );
}
