import { useEffect, useState } from "react";
import { useSelector } from "react-redux";
import PageMeta from "../components/common/PageMeta";
import PageBreadCrumb from "../components/common/PageBreadCrumb";
import api from "../services/api";
import { userService, type CreateUserRequest } from "../services/userService";
import type { UserInfo, ApiResponse } from "../services/authService";
import type { RootState } from "../store";
import Button from "../components/ui/button/Button";
import { Modal } from "../components/ui/modal";
import Label from "../components/form/Label";
import Pagination from "../components/ui/pagination/Pagination";
import { usePagination } from "../hooks/usePagination";
import Input from "../components/form/input/InputField";
import SearchableSelect from "../components/form/SearchableSelect";
import { useModal } from "../hooks/useModal";

const ROLES = [
  { value: "Super_Admin", label: "مدير النظام" },
  { value: "Ministry_Admin", label: "مسؤول الوزارة" },
  { value: "Entity_Admin", label: "مسؤول الجهة" },
  { value: "Data_Entry_User", label: "مُدخل بيانات" },
  { value: "Reviewer", label: "مراجع" },
  { value: "Auditor", label: "مدقق" },
  { value: "Viewer", label: "مشاهد" },
];

const ROLE_LABELS: Record<string, string> = Object.fromEntries(ROLES.map(r => [r.value, r.label]));

// Roles that require an entity (all except Super_Admin and Ministry_Admin).
const ROLES_REQUIRING_ENTITY = new Set([
  "Entity_Admin", "Data_Entry_User", "Reviewer", "Auditor", "Viewer",
]);

// What each caller role is allowed to assign — mirrors the backend `CanAssignRole` check.
const ASSIGNABLE_BY: Record<string, Set<string>> = {
  Super_Admin: new Set(ROLES.map(r => r.value)),
  Ministry_Admin: new Set(ROLES.filter(r => r.value !== "Super_Admin").map(r => r.value)),
  Entity_Admin: new Set(["Data_Entry_User", "Reviewer", "Auditor", "Viewer"]),
};

interface EntityFlat { id: number; nameAr: string; }
interface EntityTree { id: number; nameAr: string; childEntities: EntityTree[]; }

export default function UserManagement() {
  const { user: currentUser } = useSelector((state: RootState) => state.auth);
  const [users, setUsers] = useState<UserInfo[]>([]);
  const [entities, setEntities] = useState<EntityFlat[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState("");
  const { isOpen, openModal, closeModal } = useModal();
  const { pagination, setPage, updateFromResponse } = usePagination(10);

  const emptyForm: CreateUserRequest = {
    userName: "", email: "", fullNameAr: "", password: "", role: "Data_Entry_User", entityId: undefined,
  };
  const [form, setForm] = useState<CreateUserRequest>(emptyForm);

  const assignableRoles = ROLES.filter(r =>
    (ASSIGNABLE_BY[currentUser?.role ?? ""] ?? new Set()).has(r.value)
  );

  const loadUsers = async (page = pagination.page) => {
    setLoading(true);
    try {
      const res = await userService.getAll({ pageSize: 10, page });
      const data = res.data.data;
      setUsers(data?.items || []);
      if (data) updateFromResponse(data.totalCount, data.page, data.pageSize);
    } finally {
      setLoading(false);
    }
  };

  const loadEntities = async () => {
    try {
      const res = await api.get<ApiResponse<EntityTree[]>>("/entities");
      const flat: EntityFlat[] = [];
      const walk = (items: EntityTree[]) => {
        for (const e of items) {
          flat.push({ id: e.id, nameAr: e.nameAr });
          if (e.childEntities?.length) walk(e.childEntities);
        }
      };
      walk(res.data.data || []);
      setEntities(flat);
    } catch {
      /* ignored — entity picker will be empty */
    }
  };

  useEffect(() => { loadUsers(); }, [pagination.page]);
  useEffect(() => { loadEntities(); }, []);

  const handleOpenCreate = () => {
    setForm({ ...emptyForm, role: assignableRoles[0]?.value ?? "Data_Entry_User" });
    setFormError("");
    openModal();
  };

  const handleCreate = async () => {
    setFormError("");
    if (!form.fullNameAr.trim()) return setFormError("الاسم الكامل مطلوب");
    if (!form.userName.trim()) return setFormError("اسم المستخدم مطلوب");
    if (!form.email.trim()) return setFormError("البريد الإلكتروني مطلوب");
    if (!form.password || form.password.length < 8) return setFormError("كلمة المرور يجب أن تكون 8 أحرف على الأقل");
    if (ROLES_REQUIRING_ENTITY.has(form.role) && !form.entityId) return setFormError("يجب اختيار الجهة لهذا الدور");

    setSaving(true);
    try {
      const payload: CreateUserRequest = { ...form };
      if (!ROLES_REQUIRING_ENTITY.has(form.role)) payload.entityId = undefined;
      const res = await userService.create(payload);
      if (!res.data.success) {
        setFormError(res.data.message || "فشل إنشاء المستخدم");
        return;
      }
      closeModal();
      setForm(emptyForm);
      loadUsers();
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string; errors?: string[] } } };
      const msg = e.response?.data?.message
        || e.response?.data?.errors?.join("، ")
        || "فشل إنشاء المستخدم";
      setFormError(msg);
    } finally {
      setSaving(false);
    }
  };

  const handleDeactivate = async (u: UserInfo) => {
    if (u.id === currentUser?.id) return; // extra guard — button is hidden anyway
    if (!confirm(`هل أنت متأكد من تعطيل حساب ${u.fullNameAr}؟`)) return;
    try {
      const res = await userService.deactivate(u.id);
      if (!res.data.success) alert(res.data.message || "فشل تعطيل المستخدم");
      loadUsers();
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } };
      alert(e.response?.data?.message || "فشل تعطيل المستخدم");
    }
  };

  const entityOptions = entities.map(e => ({ value: String(e.id), label: e.nameAr }));

  return (
    <>
      <PageMeta title="إدارة المستخدمين | نظام إدارة المؤشرات" />
      <PageBreadCrumb pageTitle="إدارة المستخدمين" />

      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03] lg:p-6">
        <div className="flex items-center justify-between mb-5">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90">المستخدمون</h3>
          {assignableRoles.length > 0 && (
            <Button size="sm" onClick={handleOpenCreate}>إضافة مستخدم</Button>
          )}
        </div>

        {loading ? (
          <p className="text-gray-500 text-center py-8">جارٍ التحميل...</p>
        ) : (
          <>
          <div className="overflow-x-auto">
            <table className="w-full table-auto">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700">
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الاسم الكامل</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">اسم المستخدم</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">البريد الإلكتروني</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الدور</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الجهة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">إجراءات</th>
                </tr>
              </thead>
              <tbody>
                {users.map((u) => {
                  const isSelf = u.id === currentUser?.id;
                  return (
                    <tr key={u.id} className="border-b border-gray-100 dark:border-gray-800">
                      <td className="px-4 py-3 text-sm text-gray-800 dark:text-white/90">
                        {u.fullNameAr}
                        {isSelf && (
                          <span className="ms-2 px-2 py-0.5 text-[10px] rounded-full bg-brand-50 text-brand-600 dark:bg-brand-500/10">
                            أنت
                          </span>
                        )}
                      </td>
                      <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{u.userName}</td>
                      <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{u.email}</td>
                      <td className="px-4 py-3 text-sm">
                        <span className="px-2 py-1 text-xs rounded-full bg-brand-50 text-brand-600 dark:bg-brand-500/10">
                          {ROLE_LABELS[u.role] || u.role}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{u.entityNameAr || "—"}</td>
                      <td className="px-4 py-3 text-sm">
                        {isSelf ? (
                          <span
                            title="لا يمكنك تعطيل حسابك الشخصي"
                            className="text-gray-400 dark:text-gray-600 text-xs cursor-not-allowed"
                          >
                            تعطيل
                          </span>
                        ) : (
                          <button
                            onClick={() => handleDeactivate(u)}
                            className="text-error-500 hover:text-error-700 text-xs"
                          >
                            تعطيل
                          </button>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
          <Pagination
            currentPage={pagination.page}
            totalPages={pagination.totalPages}
            totalCount={pagination.totalCount}
            pageSize={pagination.pageSize}
            onPageChange={(p) => { setPage(p); loadUsers(p); }}
          />
          </>
        )}
      </div>

      <Modal isOpen={isOpen} onClose={closeModal} className="max-w-md">
        <div className="p-6">
          <h4 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-5">إضافة مستخدم جديد</h4>

          {formError && (
            <div className="p-3 mb-4 text-sm text-error-700 bg-error-50 rounded-lg border border-error-200 dark:bg-error-900/20 dark:text-error-400 dark:border-error-800">
              {formError}
            </div>
          )}

          <div className="space-y-4">
            <div>
              <Label>الاسم الكامل <span className="text-error-500">*</span></Label>
              <Input value={form.fullNameAr} onChange={(e) => setForm({ ...form, fullNameAr: e.target.value })} />
            </div>
            <div>
              <Label>اسم المستخدم <span className="text-error-500">*</span></Label>
              <Input value={form.userName} onChange={(e) => setForm({ ...form, userName: e.target.value })} />
            </div>
            <div>
              <Label>البريد الإلكتروني <span className="text-error-500">*</span></Label>
              <Input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
            </div>
            <div>
              <Label>كلمة المرور <span className="text-error-500">*</span></Label>
              <Input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
              <p className="mt-1 text-xs text-gray-500">8 أحرف على الأقل، وتشمل حرفاً كبيراً ورقماً ورمزاً.</p>
            </div>
            <div>
              <Label>الدور <span className="text-error-500">*</span></Label>
              <SearchableSelect
                options={assignableRoles}
                value={form.role}
                onChange={(val) => setForm({ ...form, role: val, entityId: ROLES_REQUIRING_ENTITY.has(val) ? form.entityId : undefined })}
                placeholder="اختر الدور"
                searchPlaceholder="بحث عن الدور..."
              />
            </div>
            {ROLES_REQUIRING_ENTITY.has(form.role) && (
              <div>
                <Label>الجهة <span className="text-error-500">*</span></Label>
                <SearchableSelect
                  options={entityOptions}
                  value={form.entityId ? String(form.entityId) : ""}
                  onChange={(val) => setForm({ ...form, entityId: val ? Number(val) : undefined })}
                  placeholder="اختر الجهة"
                  searchPlaceholder="بحث عن الجهة..."
                />
              </div>
            )}
            <div className="flex gap-3 pt-4 justify-start">
              <Button size="sm" onClick={handleCreate} disabled={saving}>
                {saving ? "جارٍ الإنشاء..." : "إنشاء"}
              </Button>
              <Button size="sm" variant="outline" onClick={closeModal}>إلغاء</Button>
            </div>
          </div>
        </div>
      </Modal>
    </>
  );
}
