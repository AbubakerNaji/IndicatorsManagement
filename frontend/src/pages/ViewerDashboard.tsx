import { useEffect, useState } from "react";
import PageMeta from "../components/common/PageMeta";
import { publicationService, type PublicationStatistics } from "../services/publicationService";
import { entryService, type IndicatorEntryResponse } from "../services/entryService";

export default function ViewerDashboard() {
  const [stats, setStats] = useState<PublicationStatistics | null>(null);
  const [entries, setEntries] = useState<IndicatorEntryResponse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      try {
        const [statsRes, entriesRes] = await Promise.all([
          publicationService.getStatistics(),
          entryService.getAll({ pageSize: 20 }),
        ]);
        setStats(statsRes.data.data || null);
        setEntries(entriesRes.data.data?.items || []);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading) return <p className="text-gray-500 text-center py-12">جارٍ التحميل...</p>;

  return (
    <>
      <PageMeta title="البيانات المنشورة | نظام إدارة المؤشرات" />

      <div className="mb-6">
        <div className="flex items-center gap-2 mb-2">
          <span className="px-2 py-1 text-xs rounded-full bg-brand-100 text-brand-600">عرض عام</span>
          <h2 className="text-xl font-semibold text-gray-800 dark:text-white/90">البيانات المنشورة</h2>
        </div>
        <p className="text-sm text-gray-500">عرض البيانات الإحصائية المعتمدة والمنشورة</p>
      </div>

      {/* KPI Cards */}
      {stats && (
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-4 mb-8">
          <KPI label="المؤشرات المنشورة" value={stats.totalPublished} color="text-success-500" />
          <KPI label="بانتظار النشر" value={stats.totalUnpublished} color="text-warning-500" />
          <KPI label="نسبة النشر" value={`${stats.publicationRate}%`} color="text-brand-500" />
          <KPI label="آخر نشر" value={stats.latestPublicationDate ? new Date(stats.latestPublicationDate).toLocaleDateString("ar-LY") : "—"} color="text-gray-600" />
        </div>
      )}

      {/* Published entries */}
      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03]">
        <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-4">أحدث البيانات المنشورة</h3>

        {entries.length === 0 ? (
          <p className="text-gray-400 text-center py-8">لا توجد بيانات منشورة</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full table-auto">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700">
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">المؤشر</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الجهة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الفترة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">القيمة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الوحدة</th>
                </tr>
              </thead>
              <tbody>
                {entries.map((e) => (
                  <tr key={e.id} className="border-b border-gray-100 dark:border-gray-800">
                    <td className="px-4 py-3 text-sm text-gray-800 dark:text-white/90">{e.indicatorNameAr}</td>
                    <td className="px-4 py-3 text-sm text-gray-600">{e.entityNameAr}</td>
                    <td className="px-4 py-3 text-sm text-gray-600">{e.periodDisplayNameAr}</td>
                    <td className="px-4 py-3 text-sm font-semibold text-gray-800 dark:text-white">{e.valueNumeric ?? e.valueText ?? "—"}</td>
                    <td className="px-4 py-3 text-sm text-gray-500">{e.unitSnapshot ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </>
  );
}

function KPI({ label, value, color }: { label: string; value: string | number; color: string }) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-4 dark:border-gray-800 dark:bg-white/[0.03]">
      <p className={`text-2xl font-bold ${color}`}>{value}</p>
      <p className="text-xs text-gray-500 mt-1">{label}</p>
    </div>
  );
}
