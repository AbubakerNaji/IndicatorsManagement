import { useEffect, useState } from "react";
import PageMeta from "../components/common/PageMeta";
import PageBreadCrumb from "../components/common/PageBreadCrumb";
import { notificationService, type NotificationResponse } from "../services/notificationService";
import Button from "../components/ui/button/Button";
import Pagination from "../components/ui/pagination/Pagination";
import { usePagination } from "../hooks/usePagination";
import SearchableSelect from "../components/form/SearchableSelect";

const TYPE_LABELS: Record<string, string> = {
  Due_Date: "موعد تسليم", Overdue: "متأخر", Workflow_Change: "تغيير حالة",
};

const TYPE_COLORS: Record<string, string> = {
  Due_Date: "bg-warning-50 text-warning-600", Overdue: "bg-error-50 text-error-600", Workflow_Change: "bg-blue-light-50 text-blue-light-600",
};

export default function NotificationsCenter() {
  const [notifications, setNotifications] = useState<NotificationResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState("");
  const { pagination, setPage, updateFromResponse } = usePagination(10);

  const load = async (page = pagination.page) => {
    setLoading(true);
    try {
      const params: Record<string, string | number> = { pageSize: 10, page };
      if (filter) params.type = filter;
      const res = await notificationService.getAll(params);
      const data = res.data.data;
      setNotifications(data?.items || []);
      if (data) updateFromResponse(data.totalCount, data.page, data.pageSize);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { load(); }, [filter, pagination.page]);

  const handleMarkRead = async (id: number) => {
    await notificationService.markAsRead(id);
    load();
  };

  const handleMarkAllRead = async () => {
    await notificationService.markAllAsRead();
    load();
  };

  const unreadCount = notifications.filter(n => !n.isRead).length;

  return (
    <>
      <PageMeta title="الإشعارات | نظام إدارة المؤشرات" />
      <PageBreadCrumb pageTitle="مركز الإشعارات" />

      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03] lg:p-6">
        <div className="flex items-center justify-between mb-5 flex-wrap gap-3">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90">
            الإشعارات {unreadCount > 0 && <span className="text-sm text-error-500">({unreadCount} غير مقروء)</span>}
          </h3>
          <div className="flex items-center gap-3">
            <SearchableSelect
              options={[
                { value: "", label: "الكل" },
                { value: "Due_Date", label: "مواعيد التسليم" },
                { value: "Overdue", label: "المتأخرات" },
                { value: "Workflow_Change", label: "تغييرات الحالة" },
              ]}
              value={filter}
              onChange={setFilter}
              placeholder="نوع الإشعار"
              searchPlaceholder="بحث..."
              className="w-48"
            />
            {unreadCount > 0 && (
              <Button size="sm" variant="outline" onClick={handleMarkAllRead}>تعليم الكل كمقروء</Button>
            )}
          </div>
        </div>

        {loading ? (
          <p className="text-gray-500 text-center py-8">جارٍ التحميل...</p>
        ) : notifications.length === 0 ? (
          <p className="text-gray-400 text-center py-8">لا توجد إشعارات</p>
        ) : (
          <>
          <div className="space-y-2">
            {notifications.map((n) => (
              <div key={n.id}
                className={`p-4 rounded-xl border transition-colors ${n.isRead
                  ? "border-gray-100 bg-white dark:border-gray-800 dark:bg-transparent"
                  : "border-brand-200 bg-brand-25 dark:border-brand-800 dark:bg-brand-500/5"}`}>
                <div className="flex items-start justify-between gap-3">
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                      <span className={`px-2 py-0.5 text-xs rounded-full ${TYPE_COLORS[n.notificationType] || "bg-gray-100 text-gray-500"}`}>
                        {TYPE_LABELS[n.notificationType] || n.notificationType}
                      </span>
                      {!n.isRead && <span className="w-2 h-2 rounded-full bg-brand-500" />}
                    </div>
                    <h4 className="text-sm font-medium text-gray-800 dark:text-white/90">{n.titleAr}</h4>
                    <p className="text-xs text-gray-500 mt-1">{n.messageAr}</p>
                    <p className="text-xs text-gray-400 mt-2">{new Date(n.sentAt).toLocaleString("ar-LY")}</p>
                  </div>
                  {!n.isRead && (
                    <button onClick={() => handleMarkRead(n.id)} className="text-xs text-brand-500 hover:text-brand-700 whitespace-nowrap">
                      تعليم كمقروء
                    </button>
                  )}
                </div>
              </div>
            ))}
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
    </>
  );
}
