import { useEffect, useState } from "react";
import PageMeta from "../components/common/PageMeta";
import PageBreadCrumb from "../components/common/PageBreadCrumb";
import api from "../services/api";
import type { ApiResponse } from "../services/authService";
import Button from "../components/ui/button/Button";
import Pagination from "../components/ui/pagination/Pagination";
import SearchableSelect from "../components/form/SearchableSelect";
import { Modal } from "../components/ui/modal";
import { useModal } from "../hooks/useModal";
import Label from "../components/form/Label";
import Input from "../components/form/input/InputField";

interface ReportingPeriodResponse {
  id: number;
  periodType: string;
  year: number;
  month: number | null;
  quarter: number | null;
  halfYear: number | null;
  startDate: string;
  endDate: string;
  displayNameAr: string;
  isOpen: boolean;
}

const PERIOD_TYPES = [
  { value: "", label: "الكل" },
  { value: "Monthly", label: "شهري" },
  { value: "Quarterly", label: "ربع سنوي" },
  { value: "Semi_Annual", label: "نصف سنوي" },
  { value: "Annual", label: "سنوي" },
];

const GENERATE_TYPES = [
  { key: "Monthly", label: "شهري" },
  { key: "Quarterly", label: "ربع سنوي" },
  { key: "Semi_Annual", label: "نصف سنوي" },
  { key: "Annual", label: "سنوي" },
];

export default function ReportingPeriods() {
  const [periods, setPeriods] = useState<ReportingPeriodResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [filterType, setFilterType] = useState("");
  const [filterYear, setFilterYear] = useState("");
  const [currentPage, setCurrentPage] = useState(1);
  const pageSize = 12;
  const { isOpen, openModal, closeModal } = useModal();
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [genForm, setGenForm] = useState({ startYear: new Date().getFullYear() + 1, endYear: new Date().getFullYear() + 1, periodTypes: ["Monthly", "Quarterly", "Semi_Annual", "Annual"] });
  const [genResult, setGenResult] = useState<number | null>(null);

  const loadPeriods = async () => {
    setLoading(true);
    try {
      const params: Record<string, string> = {};
      if (filterType) params.periodType = filterType;
      if (filterYear) params.year = filterYear;
      const res = await api.get<ApiResponse<ReportingPeriodResponse[]>>("/reporting-periods", { params });
      setPeriods(res.data.data || []);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadPeriods(); }, [filterType, filterYear]);

  const totalPages = Math.ceil(periods.length / pageSize);
  const paginatedPeriods = periods.slice((currentPage - 1) * pageSize, currentPage * pageSize);

  const togglePeriodType = (type: string) => {
    setGenForm(prev => ({
      ...prev,
      periodTypes: prev.periodTypes.includes(type)
        ? prev.periodTypes.filter(t => t !== type)
        : [...prev.periodTypes, type],
    }));
  };

  const handleGenerate = async () => {
    if (genForm.periodTypes.length === 0) { setError("يرجى اختيار نوع واحد على الأقل"); return; }
    if (genForm.startYear > genForm.endYear) { setError("سنة البداية يجب أن تكون أقل من أو تساوي سنة النهاية"); return; }
    setSaving(true);
    setError("");
    setGenResult(null);
    try {
      const res = await api.post<ApiResponse<number>>("/reporting-periods/generate", {
        startYear: genForm.startYear,
        endYear: genForm.endYear,
        periodTypes: genForm.periodTypes,
      });
      if (res.data.success) {
        setGenResult(res.data.data || 0);
        loadPeriods();
      } else {
        setError(res.data.message || "حدث خطأ");
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message;
      setError(msg || "حدث خطأ أثناء التوليد");
    } finally {
      setSaving(false);
    }
  };

  return (
    <>
      <PageMeta title="فترات الإبلاغ | نظام إدارة المؤشرات" />
      <PageBreadCrumb pageTitle="فترات الإبلاغ" />

      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03] lg:p-6">
        <div className="flex items-center justify-between mb-5 gap-4 flex-wrap">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90">فترات الإبلاغ</h3>
          <div className="flex items-center gap-3">
            <SearchableSelect
              options={PERIOD_TYPES}
              value={filterType}
              onChange={setFilterType}
              placeholder="نوع الفترة"
              searchPlaceholder="بحث..."
              className="w-48"
            />
            <input
              type="number"
              placeholder="السنة"
              value={filterYear}
              onChange={(e) => setFilterYear(e.target.value)}
              className="px-3 py-2 text-sm border border-gray-200 rounded-lg w-24 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
            />
            <Button size="sm" onClick={() => { setError(""); setGenResult(null); openModal(); }}>إنشاء فترات</Button>
          </div>
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
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">السنة</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">تاريخ البداية</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">تاريخ النهاية</th>
                  <th className="px-4 py-3 text-right text-sm font-medium text-gray-500">الحالة</th>
                </tr>
              </thead>
              <tbody>
                {paginatedPeriods.map((p) => (
                  <tr key={p.id} className="border-b border-gray-100 dark:border-gray-800">
                    <td className="px-4 py-3 text-sm text-gray-800 dark:text-white/90">{p.displayNameAr}</td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">
                      {PERIOD_TYPES.find(t => t.value === p.periodType)?.label || p.periodType}
                    </td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{p.year}</td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{p.startDate}</td>
                    <td className="px-4 py-3 text-sm text-gray-600 dark:text-gray-400">{p.endDate}</td>
                    <td className="px-4 py-3 text-sm">
                      <span className={`px-2 py-1 text-xs rounded-full ${p.isOpen
                        ? "bg-success-50 text-success-600" : "bg-gray-100 text-gray-500"}`}>
                        {p.isOpen ? "مفتوحة" : "مغلقة"}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {periods.length === 0 && (
              <p className="text-gray-400 text-center py-6">لا توجد فترات</p>
            )}
            <Pagination
              currentPage={currentPage}
              totalPages={totalPages}
              totalCount={periods.length}
              pageSize={pageSize}
              onPageChange={setCurrentPage}
            />
          </div>
        )}
      </div>

      <Modal isOpen={isOpen} onClose={closeModal} className="max-w-md">
        <div className="p-6">
          <h4 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-5">إنشاء فترات إبلاغ</h4>
          {error && (
            <div className="mb-4 p-3 rounded-lg bg-error-50 text-error-600 text-sm">{error}</div>
          )}
          {genResult !== null && (
            <div className="mb-4 p-3 rounded-lg bg-success-50 text-success-600 text-sm">
              تم إنشاء {genResult} فترة بنجاح
            </div>
          )}
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <Label>سنة البداية *</Label>
                <Input type="number" value={String(genForm.startYear)} onChange={(e) => setGenForm({ ...genForm, startYear: Number(e.target.value) })} />
              </div>
              <div>
                <Label>سنة النهاية *</Label>
                <Input type="number" value={String(genForm.endYear)} onChange={(e) => setGenForm({ ...genForm, endYear: Number(e.target.value) })} />
              </div>
            </div>
            <div>
              <Label>أنواع الفترات *</Label>
              <div className="flex flex-wrap gap-3 mt-2">
                {GENERATE_TYPES.map(({ key, label }) => (
                  <label key={key} className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300 cursor-pointer">
                    <input
                      type="checkbox"
                      checked={genForm.periodTypes.includes(key)}
                      onChange={() => togglePeriodType(key)}
                      className="w-4 h-4 rounded border-gray-300"
                    />
                    {label}
                  </label>
                ))}
              </div>
            </div>
            <div className="flex gap-3 pt-4 justify-start">
              <Button size="sm" onClick={handleGenerate} disabled={saving}>
                {saving ? "جارٍ الإنشاء..." : "إنشاء"}
              </Button>
              <Button size="sm" variant="outline" onClick={closeModal}>إغلاق</Button>
            </div>
          </div>
        </div>
      </Modal>
    </>
  );
}
