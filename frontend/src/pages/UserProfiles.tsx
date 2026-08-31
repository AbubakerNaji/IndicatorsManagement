import { useSelector } from "react-redux";
import PageBreadcrumb from "../components/common/PageBreadCrumb";
import PageMeta from "../components/common/PageMeta";
import type { RootState } from "../store";

const ROLE_LABELS: Record<string, string> = {
  Super_Admin: "مدير النظام",
  Ministry_Admin: "مسؤول الوزارة",
  Entity_Admin: "مسؤول الجهة",
  Data_Entry_User: "مُدخل بيانات",
  Reviewer: "مراجع",
  Auditor: "مدقق",
  Viewer: "مشاهد",
};

function Field({ label, value }: { label: string; value: string | null | undefined }) {
  return (
    <div>
      <p className="mb-1 text-xs leading-normal text-gray-500 dark:text-gray-400">{label}</p>
      <p className="text-sm font-medium text-gray-800 dark:text-white/90">
        {value ?? "—"}
      </p>
    </div>
  );
}

export default function UserProfiles() {
  const { user } = useSelector((state: RootState) => state.auth);

  if (!user) {
    return (
      <>
        <PageMeta title="الملف الشخصي | نظام إدارة المؤشرات" />
        <PageBreadcrumb pageTitle="الملف الشخصي" />
        <div className="rounded-2xl border border-gray-200 bg-white p-6 dark:border-gray-800 dark:bg-white/[0.03]">
          <p className="text-sm text-gray-500">لا توجد بيانات مستخدم.</p>
        </div>
      </>
    );
  }

  const initial = (user.fullNameAr || user.userName || "?").trim().charAt(0);

  return (
    <>
      <PageMeta
        title="الملف الشخصي | نظام إدارة المؤشرات"
        description="ملف المستخدم الشخصي في نظام إدارة المؤشرات"
      />
      <PageBreadcrumb pageTitle="الملف الشخصي" />

      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03] lg:p-6">
        <h3 className="mb-5 text-lg font-semibold text-gray-800 dark:text-white/90 lg:mb-7">
          الملف الشخصي
        </h3>

        <div className="space-y-6">
          {/* Identity card */}
          <div className="p-5 border border-gray-200 rounded-2xl dark:border-gray-800 lg:p-6">
            <div className="flex items-center gap-5">
              <div className="flex items-center justify-center w-20 h-20 text-2xl font-semibold text-white rounded-full bg-brand-500">
                {initial}
              </div>
              <div>
                <h4 className="mb-1 text-lg font-semibold text-gray-800 dark:text-white/90">
                  {user.fullNameAr}
                </h4>
                <p className="text-sm text-gray-500 dark:text-gray-400">
                  {ROLE_LABELS[user.role] || user.role}
                  {user.entityNameAr ? ` · ${user.entityNameAr}` : ""}
                </p>
              </div>
            </div>
          </div>

          {/* Account details */}
          <div className="p-5 border border-gray-200 rounded-2xl dark:border-gray-800 lg:p-6">
            <h4 className="mb-5 text-lg font-semibold text-gray-800 dark:text-white/90">
              بيانات الحساب
            </h4>
            <div className="grid grid-cols-1 gap-4 lg:grid-cols-2 lg:gap-7">
              <Field label="الاسم الكامل" value={user.fullNameAr} />
              <Field label="اسم المستخدم" value={user.userName} />
              <Field label="البريد الإلكتروني" value={user.email} />
              <Field label="الدور" value={ROLE_LABELS[user.role] || user.role} />
              <Field label="الجهة" value={user.entityNameAr} />
              <Field label="معرف الحساب" value={String(user.id)} />
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
