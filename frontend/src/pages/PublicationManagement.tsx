import { useEffect, useState } from "react";
import PageMeta from "../components/common/PageMeta";
import PageBreadCrumb from "../components/common/PageBreadCrumb";
import { entryService, type IndicatorEntryResponse } from "../services/entryService";
import { publicationService } from "../services/publicationService";
import WorkflowBadge from "../components/ui/badge/WorkflowBadge";
import Button from "../components/ui/button/Button";
import Pagination from "../components/ui/pagination/Pagination";
import { usePagination } from "../hooks/usePagination";

export default function PublicationManagement() {
  const [entries, setEntries] = useState<IndicatorEntryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<Set<number>>(new Set());
  const { pagination, setPage, updateFromResponse } = usePagination(10);

  const load = async (page = pagination.page) => {
    setLoading(true);
    try {
      const res = await entryService.getAll({ pageSize: 10, page });
      const data = res.data.data;
      // Show only Final_Approved entries
      const items = (data?.items || []).filter(e => e.workflowState === "Final_Approved");
      setEntries(items);
      if (data) updateFromResponse(data.totalCount, data.page, data.pageSize);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [pagination.page]);

  const toggleSelect = (id: number) => {
    setSelected(prev => {
      const next = new Set(prev);
      if (next.has(id)) {
        next.delete(id);
      } else {
        next.add(id);
      }
      return next;
    });
  };

  const toggleSelectAll = () => {
    if (selected.size === entries.length) setSelected(new Set());
    else setSelected(new Set(entries.map(e => e.id)));
  };

  const handlePublish = async (id: number) => {
    await publicationService.publish(id);
    load();
  };

  const handleUnpublish = async (id: number) => {
    await publicationService.unpublish(id);
    load();
  };

  const handleBulkPublish = async () => {
    if (selected.size === 0) return;
    await publicationService.bulkPublish([...selected], "نشر جماعي");
    setSelected(new Set());
    load();
  };

  const isPublished = (e: IndicatorEntryResponse) =>
    (e as unknown as { publicationStatus?: string }).publicationStatus === "Published";

  return (
    <>
      <PageMeta title="إدارة النشر | نظام إدارة المؤشرات" />
      <PageBreadCrumb pageTitle="إدارة النشر" />

      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03] lg:p-6">
        <div className="flex items-center justify-between mb-5 flex-wrap gap-3">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90">
            الإدخالات المعتمدة نهائياً
          </h3>
          {selected.size > 0 && (
            <div className="flex gap-2">
              <Button size="sm" onClick={handleBulkPublish}>
                نشر المحدد ({selected.size})
              </Button>
            </div>
          )}
        </div>

        {loading ? (
          <p className="text-gray-500 text-center py-8">جارٍ التحميل...</p>
        ) : entries.length === 0 ? (
          <p className="text-gray-400 text-center py-8">لا توجد إدخالات معتمدة</p>
        ) : (
          <>
          <div className="overflow-x-auto">
            <table className="w-full table-auto">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700">
                  <th className="px-4 py-3 text-right">
                    <input type="checkbox" checked={selected.size === entries.length} onChange={toggleSelectAll}
                      className="rounded border-gray-300" />
                  </th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">المؤشر</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الجهة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الفترة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">القيمة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الحالة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">النشر</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">إجراءات</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((e) => (
                  <tr key={e.id} className="border-b border-gray-100 dark:border-gray-800">
                    <td className="px-4 py-3">
                      <input type="checkbox" checked={selected.has(e.id)} onChange={() => toggleSelect(e.id)}
                        className="rounded border-gray-300" />
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-800 dark:text-white/90">{e.indicatorNameAr}</td>
                    <td className="px-4 py-3 text-sm text-gray-600">{e.entityNameAr}</td>
                    <td className="px-4 py-3 text-sm text-gray-600">{e.periodDisplayNameAr}</td>
                    <td className="px-4 py-3 text-sm font-mono">{e.valueNumeric ?? e.valueText ?? "—"}</td>
                    <td className="px-4 py-3"><WorkflowBadge state={e.workflowState} /></td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-1 text-xs rounded-full ${isPublished(e) ? "bg-success-100 text-success-700" : "bg-gray-100 text-gray-500"}`}>
                        {isPublished(e) ? "منشور" : "غير منشور"}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-sm">
                      {isPublished(e) ? (
                        <button onClick={() => handleUnpublish(e.id)} className="text-orange-500 hover:text-orange-700 text-xs">إلغاء النشر</button>
                      ) : (
                        <button onClick={() => handlePublish(e.id)} className="text-success-500 hover:text-success-700 text-xs">نشر</button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination currentPage={pagination.page} totalPages={pagination.totalPages}
            totalCount={pagination.totalCount} pageSize={pagination.pageSize}
            onPageChange={(p) => { setPage(p); load(p); }} />
          </>
        )}
      </div>
    </>
  );
}
