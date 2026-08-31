import { useEffect, useState } from "react";
import PageMeta from "../components/common/PageMeta";
import PageBreadCrumb from "../components/common/PageBreadCrumb";
import { indicatorService, type IndicatorResponse } from "../services/indicatorService";
import Button from "../components/ui/button/Button";
import Pagination from "../components/ui/pagination/Pagination";
import { usePagination } from "../hooks/usePagination";
import { Modal } from "../components/ui/modal";
import { useModal } from "../hooks/useModal";
import Label from "../components/form/Label";
import Input from "../components/form/input/InputField";
import TextArea from "../components/form/input/TextArea";
import SearchableSelect from "../components/form/SearchableSelect";

const FREQUENCY_LABELS: Record<string, string> = {
  Monthly: "شهري", Quarterly: "ربع سنوي", Semi_Annual: "نصف سنوي", Annual: "سنوي",
};

const FREQUENCY_OPTIONS = [
  { value: "Monthly", label: "شهري" },
  { value: "Quarterly", label: "ربع سنوي" },
  { value: "Semi_Annual", label: "نصف سنوي" },
  { value: "Annual", label: "سنوي" },
];

interface IndicatorForm {
  code: string;
  nameAr: string;
  definitionAr: string;
  calculationMethodAr: string;
  unitAr: string;
  dataSourceAr: string;
  objectiveAr: string;
  publicationFrequency: string;
  requiresAttachment: boolean;
  requiresReview: boolean;
}

const emptyForm: IndicatorForm = {
  code: "", nameAr: "", definitionAr: "", calculationMethodAr: "",
  unitAr: "", dataSourceAr: "", objectiveAr: "", publicationFrequency: "Annual",
  requiresAttachment: false, requiresReview: true,
};

export default function IndicatorManagement() {
  const [indicators, setIndicators] = useState<IndicatorResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [search, setSearch] = useState("");
  const [error, setError] = useState("");
  const { pagination, setPage, updateFromResponse } = usePagination(10);
  const { isOpen, openModal, closeModal } = useModal();
  const [editId, setEditId] = useState<number | null>(null);
  const [form, setForm] = useState<IndicatorForm>({ ...emptyForm });
  // Detail view modal
  const { isOpen: detailOpen, openModal: openDetail, closeModal: closeDetail } = useModal();
  const [detailIndicator, setDetailIndicator] = useState<IndicatorResponse | null>(null);

  const loadIndicators = async (page = pagination.page) => {
    setLoading(true);
    try {
      const res = await indicatorService.getAll({ pageSize: 10, page });
      const data = res.data.data;
      setIndicators(data?.items || []);
      if (data) updateFromResponse(data.totalCount, data.page, data.pageSize);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadIndicators(); }, [pagination.page]);

  const filtered = indicators.filter(i =>
    i.nameAr.includes(search) || i.code.includes(search)
  );

  const handleOpenCreate = () => {
    setEditId(null);
    setForm({ ...emptyForm });
    setError("");
    openModal();
  };

  const handleOpenEdit = (ind: IndicatorResponse) => {
    setEditId(ind.id);
    setForm({
      code: ind.code,
      nameAr: ind.nameAr,
      definitionAr: ind.definitionAr,
      calculationMethodAr: ind.calculationMethodAr,
      unitAr: ind.unitAr,
      dataSourceAr: ind.dataSourceAr,
      objectiveAr: ind.objectiveAr || "",
      publicationFrequency: ind.publicationFrequency,
      requiresAttachment: ind.requiresAttachment,
      requiresReview: ind.requiresReview,
    });
    setError("");
    openModal();
  };

  const handleViewDetail = async (id: number) => {
    try {
      const res = await indicatorService.getById(id);
      if (res.data.data) {
        setDetailIndicator(res.data.data);
        openDetail();
      }
    } catch { /* ignore */ }
  };

  const handleSave = async () => {
    if (!form.nameAr || !form.definitionAr || !form.calculationMethodAr || !form.unitAr || !form.dataSourceAr) {
      setError("يرجى تعبئة جميع الحقول المطلوبة");
      return;
    }
    if (!editId && !form.code) {
      setError("رمز المؤشر مطلوب");
      return;
    }

    setSaving(true);
    setError("");
    try {
      if (editId) {
        const { code: _, ...updateData } = form;
        const res = await indicatorService.update(editId, updateData as unknown as Record<string, unknown>);
        if (!res.data.success) { setError(res.data.message || "حدث خطأ"); return; }
      } else {
        const res = await indicatorService.create(form as unknown as Record<string, unknown>);
        if (!res.data.success) { setError(res.data.message || "حدث خطأ"); return; }
      }
      closeModal();
      loadIndicators();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || "حدث خطأ أثناء الحفظ");
    } finally {
      setSaving(false);
    }
  };

  const handleDelete = async (id: number) => {
    if (confirm("هل أنت متأكد من حذف هذا المؤشر؟")) {
      const res = await indicatorService.delete(id);
      if (res.data.success) loadIndicators();
      else alert(res.data.message);
    }
  };

  return (
    <>
      <PageMeta title="إدارة المؤشرات | نظام إدارة المؤشرات" />
      <PageBreadCrumb pageTitle="إدارة المؤشرات" />

      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03] lg:p-6">
        <div className="flex items-center justify-between mb-5 gap-4">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90">المؤشرات</h3>
          <div className="flex items-center gap-3">
            <input
              type="text"
              placeholder="بحث ��الاسم أو الرمز..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="px-3 py-2 text-sm border border-gray-200 rounded-lg dark:border-gray-700 dark:bg-gray-800 dark:text-white"
            />
            <Button size="sm" onClick={handleOpenCreate}>إضافة مؤشر</Button>
          </div>
        </div>

        {loading ? (
          <p className="text-gray-500 text-center py-8">جارٍ التحميل...</p>
        ) : (
          <>
          <div className="overflow-x-auto">
            <table className="w-full table-auto">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700">
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الرمز</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الاسم</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">وحدة القياس</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الدورية</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الحالة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الأبعاد</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">إجراءات</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((ind) => (
                  <tr key={ind.id} className="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-white/[0.02]">
                    <td className="px-4 py-3 text-sm font-mono text-gray-800 dark:text-white/90">{ind.code}</td>
                    <td className="px-4 py-3 text-sm text-gray-800 dark:text-white/90">
                      <button onClick={() => handleViewDetail(ind.id)} className="hover:text-brand-500 hover:underline text-right">
                        {ind.nameAr}
                      </button>
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{ind.unitAr}</td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                      {FREQUENCY_LABELS[ind.publicationFrequency] || ind.publicationFrequency}
                    </td>
                    <td className="px-4 py-3 text-sm">
                      <span className={`px-2 py-1 text-xs rounded-full ${ind.isActive
                        ? "bg-success-50 text-success-600" : "bg-gray-100 text-gray-500"}`}>
                        {ind.isActive ? "فعّال" : "معطّل"}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{ind.dimensions.length}</td>
                    <td className="px-4 py-3 text-sm flex gap-2">
                      <button onClick={() => handleOpenEdit(ind)}
                        className="text-brand-500 hover:text-brand-700 text-xs">تعديل</button>
                      <button onClick={() => handleDelete(ind.id)}
                        className="text-error-500 hover:text-error-700 text-xs">حذف</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {filtered.length === 0 && (
              <p className="text-gray-400 text-center py-6">لا توجد مؤشرات</p>
            )}
          </div>
            <Pagination
              currentPage={pagination.page}
              totalPages={pagination.totalPages}
              totalCount={pagination.totalCount}
              pageSize={pagination.pageSize}
              onPageChange={(p) => { setPage(p); loadIndicators(p); }}
            />
          </>
        )}
      </div>

      {/* Create / Edit Modal */}
      <Modal isOpen={isOpen} onClose={closeModal} className="max-w-2xl">
        <div className="p-6 max-h-[85vh] overflow-y-auto">
          <h4 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-5">
            {editId ? "تعديل مؤشر" : "إضافة مؤشر جديد"}
          </h4>
          {error && (
            <div className="mb-4 p-3 rounded-lg bg-error-50 text-error-600 text-sm">{error}</div>
          )}
          <div className="space-y-4">
            {!editId && (
              <div>
                <Label>رمز المؤشر *</Label>
                <Input value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} placeholder="مثال: F-01" />
              </div>
            )}
            <div>
              <Label>اسم المؤشر *</Label>
              <Input value={form.nameAr} onChange={(e) => setForm({ ...form, nameAr: e.target.value })} />
            </div>
            <div>
              <Label>تعريف المؤشر *</Label>
              <TextArea value={form.definitionAr} onChange={(val) => setForm({ ...form, definitionAr: val })} rows={3} />
            </div>
            <div>
              <Label>آلية الاحتساب *</Label>
              <TextArea value={form.calculationMethodAr} onChange={(val) => setForm({ ...form, calculationMethodAr: val })} rows={3} />
            </div>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>وحدة القياس *</Label>
                <Input value={form.unitAr} onChange={(e) => setForm({ ...form, unitAr: e.target.value })} />
              </div>
              <div>
                <Label>دورية النشر *</Label>
                <SearchableSelect
                  options={FREQUENCY_OPTIONS}
                  value={form.publicationFrequency}
                  onChange={(val) => setForm({ ...form, publicationFrequency: val })}
                  placeholder="اختر الدورية"
                  searchPlaceholder="بحث..."
                />
              </div>
            </div>
            <div>
              <Label>مصدر البيانات *</Label>
              <Input value={form.dataSourceAr} onChange={(e) => setForm({ ...form, dataSourceAr: e.target.value })} />
            </div>
            <div>
              <Label>الهدف</Label>
              <TextArea value={form.objectiveAr} onChange={(val) => setForm({ ...form, objectiveAr: val })} rows={2} />
            </div>
            <div className="flex gap-6">
              <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 cursor-pointer">
                <input type="checkbox" checked={form.requiresAttachment}
                  onChange={(e) => setForm({ ...form, requiresAttachment: e.target.checked })}
                  className="w-4 h-4 rounded border-gray-300" />
                يتطلب مرفقات
              </label>
              <label className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 cursor-pointer">
                <input type="checkbox" checked={form.requiresReview}
                  onChange={(e) => setForm({ ...form, requiresReview: e.target.checked })}
                  className="w-4 h-4 rounded border-gray-300" />
                يتطلب مراجعة
              </label>
            </div>
            <div className="flex gap-3 pt-4 justify-start">
              <Button size="sm" onClick={handleSave} disabled={saving}>
                {saving ? "جارٍ الحفظ..." : editId ? "حفظ التعديلات" : "إنشاء"}
              </Button>
              <Button size="sm" variant="outline" onClick={closeModal}>إلغاء</Button>
            </div>
          </div>
        </div>
      </Modal>

      {/* Detail View Modal */}
      <Modal isOpen={detailOpen} onClose={closeDetail} className="max-w-2xl">
        {detailIndicator && (
          <div className="p-6 max-h-[85vh] overflow-y-auto">
            <div className="flex items-center gap-3 mb-5">
              <span className="px-3 py-1 text-sm font-mono rounded-lg bg-brand-50 text-brand-600 dark:bg-brand-500/10">
                {detailIndicator.code}
              </span>
              <h4 className="text-lg font-semibold text-gray-800 dark:text-white/90">
                {detailIndicator.nameAr}
              </h4>
            </div>
            <div className="space-y-4">
              <DetailField label="التعريف" value={detailIndicator.definitionAr} />
              <DetailField label="آلية الاحتساب" value={detailIndicator.calculationMethodAr} />
              <div className="grid grid-cols-2 gap-4">
                <DetailField label="وحدة القياس" value={detailIndicator.unitAr} />
                <DetailField label="دورية النشر" value={FREQUENCY_LABELS[detailIndicator.publicationFrequency] || detailIndicator.publicationFrequency} />
              </div>
              <DetailField label="مصدر البيانات" value={detailIndicator.dataSourceAr} />
              {detailIndicator.objectiveAr && <DetailField label="الهدف" value={detailIndicator.objectiveAr} />}
              <div className="flex gap-4 text-sm text-gray-500">
                <span>المرفقات: {detailIndicator.requiresAttachment ? "مطلوبة" : "غير مطلوبة"}</span>
                <span>المراجعة: {detailIndicator.requiresReview ? "مطلوبة" : "غير مطلوبة"}</span>
              </div>
              {detailIndicator.dimensions.length > 0 && (
                <div>
                  <h5 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">الأبعاد ({detailIndicator.dimensions.length})</h5>
                  <div className="space-y-2">
                    {detailIndicator.dimensions.map((d) => (
                      <div key={d.id} className="p-3 rounded-lg bg-gray-50 dark:bg-gray-800">
                        <span className="text-sm font-medium text-gray-800 dark:text-white/90">{d.dimensionNameAr}</span>
                        <span className="text-xs text-gray-500 mr-2">({d.dimensionType})</span>
                        {d.values.length > 0 && (
                          <div className="flex flex-wrap gap-1 mt-1">
                            {d.values.map((v) => (
                              <span key={v.id} className="px-2 py-0.5 text-xs rounded bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-300">{v.valueAr}</span>
                            ))}
                          </div>
                        )}
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>
            <div className="flex gap-3 pt-5 justify-start">
              <Button size="sm" onClick={() => { closeDetail(); handleOpenEdit(detailIndicator); }}>تعديل</Button>
              <Button size="sm" variant="outline" onClick={closeDetail}>إغلاق</Button>
            </div>
          </div>
        )}
      </Modal>
    </>
  );
}

function DetailField({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <p className="text-xs font-medium text-gray-500 dark:text-gray-400 mb-1">{label}</p>
      <p className="text-sm text-gray-800 dark:text-white/90 leading-relaxed">{value}</p>
    </div>
  );
}
