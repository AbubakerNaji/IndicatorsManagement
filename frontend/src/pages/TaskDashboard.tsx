import { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import { useNavigate } from "react-router";
import PageMeta from "../components/common/PageMeta";
import api from "../services/api";
import type { ApiResponse } from "../services/authService";
import type { RootState } from "../store";
import WorkflowBadge from "../components/ui/badge/WorkflowBadge";

interface TaskItem {
  obligationId: number;
  indicatorId: number;
  indicatorNameAr: string;
  indicatorCode: string;
  reportingPeriodId: number;
  periodDisplayNameAr: string;
  dueDate: string;
  status: string;
  entryId: number | null;
  entryWorkflowState: string | null;
  priority: string;
}

const PRIORITY_CONFIG: Record<string, { label: string; color: string }> = {
  overdue: { label: "متأخر", color: "border-r-error-500" },
  due_this_week: { label: "مستحق هذا الأسبوع", color: "border-r-warning-500" },
  due_this_month: { label: "مستحق هذا الشهر", color: "border-r-blue-light-500" },
  completed: { label: "مكتمل", color: "border-r-success-500" },
};

export default function TaskDashboard() {
  const { user } = useSelector((state: RootState) => state.auth);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    const load = async () => {
      setLoading(true);
      try {
        const res = await api.get<ApiResponse<TaskItem[]>>("/dashboard/tasks");
        setTasks(res.data.data || []);
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const grouped = {
    overdue: tasks.filter(t => t.priority === "overdue"),
    due_this_week: tasks.filter(t => t.priority === "due_this_week"),
    due_this_month: tasks.filter(t => t.priority === "due_this_month"),
    completed: tasks.filter(t => t.priority === "completed"),
  };

  return (
    <>
      <PageMeta title="مهامي | نظام إدارة المؤشرات" />

      <div className="mb-6">
        <h2 className="text-xl font-semibold text-gray-800 dark:text-white/90">
          مرحباً، {user?.fullNameAr}
        </h2>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">إليك مهامك المعلّقة</p>
      </div>

      {/* Summary badges */}
      <div className="grid grid-cols-2 gap-4 sm:grid-cols-4 mb-6">
        {(["overdue", "due_this_week", "due_this_month", "completed"] as const).map((key) => (
          <div key={key} className="rounded-xl border border-gray-200 bg-white p-4 dark:border-gray-800 dark:bg-white/[0.03]">
            <p className="text-2xl font-bold text-gray-800 dark:text-white">{grouped[key].length}</p>
            <p className="text-xs text-gray-500 mt-1">{PRIORITY_CONFIG[key].label}</p>
          </div>
        ))}
      </div>

      {loading ? (
        <p className="text-gray-500 text-center py-8">جارٍ التحميل...</p>
      ) : tasks.length === 0 ? (
        <div className="text-center py-12 text-gray-400">
          <p className="text-lg">لا توجد مهام معلّقة</p>
        </div>
      ) : (
        <div className="space-y-6">
          {(["overdue", "due_this_week", "due_this_month", "completed"] as const).map((priority) => {
            const items = grouped[priority];
            if (items.length === 0) return null;
            return (
              <div key={priority}>
                <h3 className="text-sm font-medium text-gray-500 mb-3">{PRIORITY_CONFIG[priority].label} ({items.length})</h3>
                <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                  {items.map((task) => (
                    <div
                      key={task.obligationId}
                      className={`rounded-xl border border-gray-200 bg-white p-4 border-r-4 ${PRIORITY_CONFIG[task.priority].color} dark:border-gray-800 dark:bg-white/[0.03] cursor-pointer hover:shadow-md transition-shadow`}
                      onClick={() => task.entryId ? navigate(`/entries/${task.entryId}`) : navigate(`/entries/new?indicatorId=${task.indicatorId}&periodId=${task.reportingPeriodId}`)}
                    >
                      <div className="flex items-start justify-between mb-2">
                        <h4 className="text-sm font-medium text-gray-800 dark:text-white/90">{task.indicatorNameAr}</h4>
                        {task.entryWorkflowState && <WorkflowBadge state={task.entryWorkflowState} />}
                      </div>
                      <p className="text-xs text-gray-500 mb-1">{task.periodDisplayNameAr}</p>
                      <p className="text-xs text-gray-400">الموعد: {task.dueDate}</p>
                      <div className="mt-3">
                        <button className="text-xs text-brand-500 hover:text-brand-700 font-medium">
                          {task.entryId ? "عرض الإدخال" : "إنشاء إدخال"}
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </>
  );
}
