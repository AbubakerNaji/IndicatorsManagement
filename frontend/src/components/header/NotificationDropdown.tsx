import { useEffect, useState } from "react";
import { Dropdown } from "../ui/dropdown/Dropdown";
import { Link } from "react-router";
import { notificationService, type NotificationResponse } from "../../services/notificationService";
import { useSelector } from "react-redux";
import type { RootState } from "../../store";

const TYPE_ICONS: Record<string, string> = {
  Due_Date: "⏰",
  Overdue: "⚠️",
  Workflow_Change: "🔄",
};

export default function NotificationDropdown() {
  const [isOpen, setIsOpen] = useState(false);
  const [notifications, setNotifications] = useState<NotificationResponse[]>([]);
  const { isAuthenticated } = useSelector((state: RootState) => state.auth);

  const unreadCount = notifications.filter((n) => !n.isRead).length;

  useEffect(() => {
    if (!isAuthenticated) return;
    const load = async () => {
      try {
        const res = await notificationService.getAll({ pageSize: 5 });
        setNotifications(res.data.data?.items || []);
      } catch {
        /* ignore */
      }
    };
    load();
  }, [isAuthenticated, isOpen]);

  const handleMarkRead = async (id: number) => {
    await notificationService.markAsRead(id);
    setNotifications((prev) =>
      prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
    );
  };

  return (
    <div className="relative">
      <button
        className="relative flex items-center justify-center text-gray-500 transition-colors bg-white border border-gray-200 rounded-full hover:text-gray-700 h-11 w-11 hover:bg-gray-100 dark:border-gray-800 dark:bg-gray-900 dark:text-gray-400 dark:hover:bg-gray-800"
        onClick={() => setIsOpen(!isOpen)}
      >
        {unreadCount > 0 && (
          <span className="absolute right-0 top-0.5 z-10 flex h-4 w-4 items-center justify-center rounded-full bg-error-500 text-[10px] text-white font-bold">
            {unreadCount}
          </span>
        )}
        <svg className="fill-current" width="20" height="20" viewBox="0 0 20 20" xmlns="http://www.w3.org/2000/svg">
          <path fillRule="evenodd" clipRule="evenodd" d="M10.75 2.29248C10.75 1.87827 10.4143 1.54248 10 1.54248C9.58583 1.54248 9.25004 1.87827 9.25004 2.29248V2.83613C6.08266 3.20733 3.62504 5.9004 3.62504 9.16748V14.4591H3.33337C2.91916 14.4591 2.58337 14.7949 2.58337 15.2091C2.58337 15.6234 2.91916 15.9591 3.33337 15.9591H4.37504H15.625H16.6667C17.0809 15.9591 17.4167 15.6234 17.4167 15.2091C17.4167 14.7949 17.0809 14.4591 16.6667 14.4591H16.375V9.16748C16.375 5.9004 13.9174 3.20733 10.75 2.83613V2.29248ZM14.875 14.4591V9.16748C14.875 6.47509 12.6924 4.29248 10 4.29248C7.30765 4.29248 5.12504 6.47509 5.12504 9.16748V14.4591H14.875ZM8.00004 17.7085C8.00004 18.1228 8.33583 18.4585 8.75004 18.4585H11.25C11.6643 18.4585 12 18.1228 12 17.7085C12 17.2943 11.6643 16.9585 11.25 16.9585H8.75004C8.33583 16.9585 8.00004 17.2943 8.00004 17.7085Z" fill="currentColor" />
        </svg>
      </button>

      <Dropdown
        isOpen={isOpen}
        onClose={() => setIsOpen(false)}
        className="absolute left-0 mt-[17px] flex h-auto max-h-[480px] w-[350px] flex-col rounded-2xl border border-gray-200 bg-white p-3 shadow-theme-lg dark:border-gray-800 dark:bg-gray-dark z-[99999]"
      >
        <div className="flex items-center justify-between pb-3 mb-3 border-b border-gray-100 dark:border-gray-700">
          <h5 className="text-lg font-semibold text-gray-800 dark:text-gray-200">
            الإشعارات
          </h5>
          <button onClick={() => setIsOpen(false)} className="text-gray-400 hover:text-gray-600">
            ✕
          </button>
        </div>

        <ul className="flex flex-col overflow-y-auto">
          {notifications.length === 0 ? (
            <li className="py-8 text-center text-gray-400 text-sm">لا توجد إشعارات</li>
          ) : (
            notifications.map((n) => (
              <li
                key={n.id}
                onClick={() => !n.isRead && handleMarkRead(n.id)}
                className={`rounded-lg p-3 mb-1 cursor-pointer transition-colors ${
                  n.isRead
                    ? "hover:bg-gray-50 dark:hover:bg-white/5"
                    : "bg-brand-50/50 hover:bg-brand-50 dark:bg-brand-500/5 dark:hover:bg-brand-500/10"
                }`}
              >
                <div className="flex items-start gap-3">
                  <span className="text-lg">{TYPE_ICONS[n.notificationType] || "📋"}</span>
                  <div className="flex-1">
                    <p className="text-sm font-medium text-gray-800 dark:text-white/90">{n.titleAr}</p>
                    <p className="text-xs text-gray-500 mt-1 line-clamp-2">{n.messageAr}</p>
                    <p className="text-xs text-gray-400 mt-1">{new Date(n.sentAt).toLocaleString("ar-LY")}</p>
                  </div>
                  {!n.isRead && <span className="w-2 h-2 rounded-full bg-brand-500 mt-1 shrink-0" />}
                </div>
              </li>
            ))
          )}
        </ul>

        <Link
          to="/notifications"
          onClick={() => setIsOpen(false)}
          className="block px-4 py-2 mt-3 text-sm font-medium text-center text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-100 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-400"
        >
          عرض جميع الإشعارات
        </Link>
      </Dropdown>
    </div>
  );
}
