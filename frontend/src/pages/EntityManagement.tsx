import { useEffect, useState } from "react";
import PageMeta from "../components/common/PageMeta";
import PageBreadCrumb from "../components/common/PageBreadCrumb";
import api from "../services/api";
import type { ApiResponse } from "../services/authService";
import Button from "../components/ui/button/Button";
import { Modal } from "../components/ui/modal";
import { useModal } from "../hooks/useModal";
import Label from "../components/form/Label";
import Input from "../components/form/input/InputField";
import SearchableSelect from "../components/form/SearchableSelect";

interface EntityResponse {
  id: number;
  nameAr: string;
  nameEn: string | null;
  type: string;
  parentEntityId: number | null;
  parentEntityNameAr: string | null;
  status: string;
  childEntities: EntityResponse[];
}

const TYPE_LABELS: Record<string, string> = {
  Ministry: "وزارة", Bureau: "مكتب", Authority: "هيئة", Department: "إدارة", Administration: "مصلحة", Fund: "صندوق", Network: "شبكة",
};

const TYPE_OPTIONS = Object.entries(TYPE_LABELS).map(([value, label]) => ({ value, label }));

interface EntityForm {
  nameAr: string;
  nameEn: string;
  type: string;
  parentEntityId: number | null;
}

export default function EntityManagement() {
  const [entities, setEntities] = useState<EntityResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const { isOpen, openModal, closeModal } = useModal();
  const [form, setForm] = useState<EntityForm>({ nameAr: "", nameEn: "", type: "Bureau", parentEntityId: null });

  const loadEntities = async () => {
    setLoading(true);
    try {
      const res = await api.get<ApiResponse<EntityResponse[]>>("/entities");
      setEntities(res.data.data || []);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadEntities(); }, []);

  // Flatten entities for parent selection
  const flattenEntities = (list: EntityResponse[]): { value: string; label: string }[] => {
    const result: { value: string; label: string }[] = [];
    const walk = (items: EntityResponse[]) => {
      for (const item of items) {
        result.push({ value: String(item.id), label: item.nameAr });
        if (item.childEntities.length > 0) walk(item.childEntities);
      }
    };
    walk(list);
    return result;
  };

  const handleOpenCreate = () => {
    setForm({ nameAr: "", nameEn: "", type: "Bureau", parentEntityId: null });
    setError("");
    openModal();
  };

  const handleCreate = async () => {
    if (!form.nameAr) { setError("اسم الجهة مطلوب"); return; }
    setSaving(true);
    setError("");
    try {
      const payload = {
        nameAr: form.nameAr,
        nameEn: form.nameEn || null,
        type: form.type,
        parentEntityId: form.parentEntityId,
      };
      const res = await api.post<ApiResponse<EntityResponse>>("/entities", payload);
      if (!res.data.success) { setError(res.data.message || "حدث خطأ"); return; }
      closeModal();
      loadEntities();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || "حدث خطأ أثناء الحفظ");
    } finally {
      setSaving(false);
    }
  };

  const renderEntity = (entity: EntityResponse, level: number = 0): React.ReactNode => (
    <tr key={entity.id} className="border-b border-gray-100 dark:border-gray-800">
      <td className="px-4 py-3 text-sm text-gray-800 dark:text-white/90" style={{ paddingRight: `${level * 24 + 16}px` }}>
        {level > 0 && <span className="text-gray-400 ml-2">└</span>}
        {entity.nameAr}
      </td>
      <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
        {TYPE_LABELS[entity.type] || entity.type}
      </td>
      <td className="px-4 py-3 text-sm">
        <span className={`px-2 py-1 text-xs rounded-full ${entity.status === "active"
          ? "bg-success-50 text-success-600" : "bg-gray-100 text-gray-500"}`}>
          {entity.status === "active" ? "فعّالة" : "معطّلة"}
        </span>
      </td>
      <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
        {entity.childEntities.length}
      </td>
    </tr>
  );

  const renderEntityRows = (list: EntityResponse[], level = 0): React.ReactNode[] =>
    list.flatMap(entity => [
      renderEntity(entity, level),
      ...renderEntityRows(entity.childEntities, level + 1),
    ]);

  return (
    <>
      <PageMeta title="إدارة الجهات | نظام إدارة المؤشرات" />
      <PageBreadCrumb pageTitle="إدارة الجهات" />

      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03] lg:p-6">
        <div className="flex items-center justify-between mb-5">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90">الجهات الحكومية</h3>
          <Button size="sm" onClick={handleOpenCreate}>إضافة جهة</Button>
        </div>

        {loading ? (
          <p className="text-gray-500 text-center py-8">جارٍ التحميل...</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full table-auto">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700">
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الاسم</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">النوع</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الحالة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الجهات الفرعية</th>
                </tr>
              </thead>
              <tbody>
                {renderEntityRows(entities)}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <Modal isOpen={isOpen} onClose={closeModal} className="max-w-md">
        <div className="p-6">
          <h4 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-5">إضافة جهة جديدة</h4>
          {error && (
            <div className="mb-4 p-3 rounded-lg bg-error-50 text-error-600 text-sm">{error}</div>
          )}
          <div className="space-y-4">
            <div>
              <Label>اسم الجهة (عربي) *</Label>
              <Input value={form.nameAr} onChange={(e) => setForm({ ...form, nameAr: e.target.value })} />
            </div>
            <div>
              <Label>اسم الجهة (إنجليزي)</Label>
              <Input value={form.nameEn} onChange={(e) => setForm({ ...form, nameEn: e.target.value })} />
            </div>
            <div>
              <Label>النوع *</Label>
              <SearchableSelect
                options={TYPE_OPTIONS}
                value={form.type}
                onChange={(val) => setForm({ ...form, type: val })}
                placeholder="اختر النوع"
                searchPlaceholder="بحث..."
              />
            </div>
            <div>
              <Label>الجهة الأم</Label>
              <SearchableSelect
                options={flattenEntities(entities)}
                value={form.parentEntityId ? String(form.parentEntityId) : ""}
                onChange={(val) => setForm({ ...form, parentEntityId: val ? Number(val) : null })}
                placeholder="اختر الجهة الأم (اختياري)"
                searchPlaceholder="بحث عن الجهة..."
              />
            </div>
            <div className="flex gap-3 pt-4 justify-start">
              <Button size="sm" onClick={handleCreate} disabled={saving}>
                {saving ? "جارٍ الحفظ..." : "إنشاء"}
              </Button>
              <Button size="sm" variant="outline" onClick={closeModal}>إلغاء</Button>
            </div>
          </div>
        </div>
      </Modal>
    </>
  );
}
