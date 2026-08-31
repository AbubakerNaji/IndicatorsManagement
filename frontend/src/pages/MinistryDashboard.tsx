import { useEffect, useState } from "react";
import PageMeta from "../components/common/PageMeta";
import api from "../services/api";
import type { ApiResponse } from "../services/authService";

interface MinistryDashboard {
  totalIndicators: number;
  totalEntities: number;
  totalSubmissions: number;
  overdueCount: number;
  submissionsByEntity: { entityId: number; entityNameAr: string; totalObligations: number; submittedCount: number; submissionRate: number }[];
  workflowDistribution: Record<string, number>;
}

const WF_LABELS: Record<string, string> = {
  Draft: "مسودة", Under_Review: "قيد المراجعة", Approved_By_Entity: "معتمد من الجهة",
  Final_Approved: "معتمد نهائياً", Rejected: "مرفوض", Returned_For_Modification: "مُعاد للتعديل",
};

export default function MinistryDashboard() {
  const [data, setData] = useState<MinistryDashboard | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      try {
        const res = await api.get<ApiResponse<MinistryDashboard>>("/dashboard/ministry");
        setData(res.data.data || null);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  if (loading) return <p className="text-gray-500 text-center py-12">جارٍ التحميل...</p>;
  if (!data) return <p className="text-error-500 text-center py-12">فشل تحميل البيانات</p>;

  return (
    <>
      <PageMeta title="لوحة المتابعة | نظام إدارة المؤشرات" />

      <h2 className="text-xl font-semibold text-gray-800 dark:text-white/90 mb-6">لوحة متابعة الوزارة</h2>

      {/* KPI Cards */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4 mb-8">
        <KPICard label="المؤشرات الفعّالة" value={data.totalIndicators} color="text-brand-500" />
        <KPICard label="الجهات الفعّالة" value={data.totalEntities} color="text-blue-light-500" />
        <KPICard label="إجمالي الإدخالات" value={data.totalSubmissions} color="text-success-500" />
        <KPICard label="المتأخرات" value={data.overdueCount} color="text-error-500" />
      </div>

      {/* Workflow Distribution */}
      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03] mb-6">
        <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-4">توزيع حالات سير العمل</h3>
        <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
          {Object.entries(data.workflowDistribution).map(([state, count]) => (
            <div key={state} className="text-center p-3 rounded-lg bg-gray-50 dark:bg-gray-800">
              <p className="text-2xl font-bold text-gray-800 dark:text-white">{count}</p>
              <p className="text-xs text-gray-500 mt-1">{WF_LABELS[state] || state}</p>
            </div>
          ))}
        </div>
      </div>

      {/* Submission rates by entity */}
      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03]">
        <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-4">نسب الالتزام حسب الجهة</h3>
        <div className="space-y-4">
          {data.submissionsByEntity.map((entity) => (
            <div key={entity.entityId}>
              <div className="flex items-center justify-between mb-1">
                <span className="text-sm text-gray-800 dark:text-white/90">{entity.entityNameAr}</span>
                <span className="text-sm font-medium text-gray-600">{entity.submissionRate}%</span>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-2 dark:bg-gray-700">
                <div className="bg-brand-500 h-2 rounded-full transition-all" style={{ width: `${entity.submissionRate}%` }} />
              </div>
              <p className="text-xs text-gray-400 mt-1">{entity.submittedCount} من {entity.totalObligations}</p>
            </div>
          ))}
        </div>
      </div>
    </>
  );
}

function KPICard({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className="rounded-xl border border-gray-200 bg-white p-4 dark:border-gray-800 dark:bg-white/[0.03]">
      <p className={`text-2xl font-bold ${color}`}>{value}</p>
      <p className="text-xs text-gray-500 mt-1">{label}</p>
    </div>
  );
}
