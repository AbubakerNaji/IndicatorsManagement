import { useEffect, useState } from "react";
import PageMeta from "../components/common/PageMeta";
import PageBreadCrumb from "../components/common/PageBreadCrumb";
import api from "../services/api";
import type { ApiResponse } from "../services/authService";
import type { PaginatedResponse } from "../services/indicatorService";
import Pagination from "../components/ui/pagination/Pagination";
import { usePagination } from "../hooks/usePagination";

interface AuditLog {
  id: number;
  userId: number | null;
  entityType: string;
  entityId: number | null;
  actionType: string;
  resultStatus: string;
  oldValuesJson: string | null;
  newValuesJson: string | null;
  ipAddress: string | null;
  createdAt: string;
}

export default function AuditConsole() {
  const [logs, setLogs] = useState<AuditLog[]>([]);
  const [loading, setLoading] = useState(true);
  const [filters, setFilters] = useState({ actionType: "", entityType: "" });
  const [selected, setSelected] = useState<AuditLog | null>(null);
  const { pagination, setPage, updateFromResponse } = usePagination(15);

  const load = async (page = pagination.page) => {
    setLoading(true);
    try {
      const params: Record<string, string | number> = { pageSize: 15, page };
      if (filters.actionType) params.actionType = filters.actionType;
      if (filters.entityType) params.entityType = filters.entityType;
      const res = await api.get<ApiResponse<PaginatedResponse<AuditLog>>>("/audit-logs", { params });
      const data = res.data.data;
      setLogs(data?.items || []);
      if (data) updateFromResponse(data.totalCount, data.page, data.pageSize);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [filters]);

  return (
    <>
      <PageMeta title="سجل التدقيق | نظام إدارة المؤشرات" />
      <PageBreadCrumb pageTitle="سجل التدقيق" />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03]">
          <div className="flex items-center gap-3 mb-4 flex-wrap">
            <input placeholder="نوع الإجراء" value={filters.actionType}
              onChange={(e) => setFilters(f => ({ ...f, actionType: e.target.value }))}
              className="text-sm border border-gray-200 rounded-lg px-3 py-2 dark:border-gray-700 dark:bg-gray-800 dark:text-white" />
            <input placeholder="نوع الكيان" value={filters.entityType}
              onChange={(e) => setFilters(f => ({ ...f, entityType: e.target.value }))}
              className="text-sm border border-gray-200 rounded-lg px-3 py-2 dark:border-gray-700 dark:bg-gray-800 dark:text-white" />
          </div>

          {loading ? (
            <p className="text-gray-500 text-center py-8">جارٍ التحميل...</p>
          ) : (
            <>
            <div className="overflow-x-auto">
              <table className="w-full table-auto text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700">
                    <th className="px-3 py-2 text-right font-medium text-gray-500">التاريخ</th>
                    <th className="px-3 py-2 text-right font-medium text-gray-500">الإجراء</th>
                    <th className="px-3 py-2 text-right font-medium text-gray-500">الكيان</th>
                    <th className="px-3 py-2 text-right font-medium text-gray-500">النتيجة</th>
                    <th className="px-3 py-2 text-right font-medium text-gray-500">IP</th>
                  </tr>
                </thead>
                <tbody>
                  {logs.map((log) => (
                    <tr key={log.id} onClick={() => setSelected(log)}
                      className={`border-b border-gray-100 dark:border-gray-800 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-800 ${selected?.id === log.id ? "bg-brand-50 dark:bg-brand-500/10" : ""}`}>
                      <td className="px-3 py-2 text-gray-600 whitespace-nowrap">{new Date(log.createdAt).toLocaleString("ar-LY")}</td>
                      <td className="px-3 py-2 text-gray-800 dark:text-white/90">{log.actionType}</td>
                      <td className="px-3 py-2 text-gray-600">{log.entityType} {log.entityId ? `#${log.entityId}` : ""}</td>
                      <td className="px-3 py-2">
                        <span className={`px-2 py-0.5 text-xs rounded-full ${log.resultStatus === "Success" ? "bg-success-50 text-success-600" : "bg-error-50 text-error-600"}`}>
                          {log.resultStatus === "Success" ? "نجاح" : "فشل"}
                        </span>
                      </td>
                      <td className="px-3 py-2 text-gray-400 font-mono text-xs">{log.ipAddress || "—"}</td>
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
              onPageChange={(p) => { setPage(p); load(p); }}
            />
            </>
          )}
        </div>

        <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03]">
          {selected ? (
            <div>
              <h4 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-4">تفاصيل السجل</h4>
              <div className="space-y-3 text-sm">
                <Detail label="الإجراء" value={selected.actionType} />
                <Detail label="الكيان" value={`${selected.entityType} #${selected.entityId || "—"}`} />
                <Detail label="النتيجة" value={selected.resultStatus} />
                <Detail label="التاريخ" value={new Date(selected.createdAt).toLocaleString("ar-LY")} />
                <Detail label="IP" value={selected.ipAddress || "—"} />
                {selected.oldValuesJson && (
                  <div>
                    <span className="text-gray-500 block mb-1">القيم السابقة:</span>
                    <pre className="text-xs bg-gray-50 dark:bg-gray-800 p-2 rounded overflow-auto max-h-32 direction-ltr">{selected.oldValuesJson}</pre>
                  </div>
                )}
                {selected.newValuesJson && (
                  <div>
                    <span className="text-gray-500 block mb-1">القيم الجديدة:</span>
                    <pre className="text-xs bg-gray-50 dark:bg-gray-800 p-2 rounded overflow-auto max-h-32 direction-ltr">{selected.newValuesJson}</pre>
                  </div>
                )}
              </div>
            </div>
          ) : (
            <p className="text-gray-400 text-center py-12">اختر سجلاً لعرض التفاصيل</p>
          )}
        </div>
      </div>
    </>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between">
      <span className="text-gray-500">{label}:</span>
      <span className="text-gray-800 dark:text-white/90">{value}</span>
    </div>
  );
}
