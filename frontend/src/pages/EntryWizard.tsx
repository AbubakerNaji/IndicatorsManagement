import { useEffect, useState, useCallback, useRef } from "react";
import { useNavigate, useSearchParams } from "react-router";
import PageMeta from "../components/common/PageMeta";
import { indicatorService, type IndicatorResponse } from "../services/indicatorService";
import { entryService } from "../services/entryService";
import { draftService } from "../services/dashboardService";
import api from "../services/api";
import type { ApiResponse } from "../services/authService";
import Button from "../components/ui/button/Button";
import Label from "../components/form/Label";
import Input from "../components/form/input/InputField";
import SearchableSelect from "../components/form/SearchableSelect";

interface PeriodOption {
  id: number;
  displayNameAr: string;
  periodType: string;
}

interface EntryFormData {
  indicatorId: number;
  reportingPeriodId: number;
  valueNumeric: string;
  valueText: string;
  notes: string;
  source: string;
  dimensions: { dimensionId: number; dimensionValueId?: number; valueNumeric?: string }[];
}

const EMPTY_FORM: EntryFormData = {
  indicatorId: 0, reportingPeriodId: 0, valueNumeric: "", valueText: "", notes: "", source: "", dimensions: [],
};

export default function EntryWizard() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const [step, setStep] = useState(1);
  const [indicators, setIndicators] = useState<IndicatorResponse[]>([]);
  const [periods, setPeriods] = useState<PeriodOption[]>([]);
  const [selectedIndicator, setSelectedIndicator] = useState<IndicatorResponse | null>(null);
  const [form, setForm] = useState<EntryFormData>({ ...EMPTY_FORM });
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const autoSaveTimer = useRef<ReturnType<typeof setInterval> | null>(null);

  // Load indicators
  useEffect(() => {
    const load = async () => {
      const res = await indicatorService.getAll({ isActive: true, pageSize: 200 });
      setIndicators(res.data.data?.items || []);

      const pRes = await api.get<ApiResponse<PeriodOption[]>>("/reporting-periods");
      setPeriods(pRes.data.data || []);
    };
    load();
  }, []);

  // Pre-select from query params
  useEffect(() => {
    const indId = searchParams.get("indicatorId");
    const perIdParam = searchParams.get("periodId");
    if (indId) {
      const id = parseInt(indId);
      setForm(f => ({ ...f, indicatorId: id }));
      const ind = indicators.find(i => i.id === id);
      if (ind) setSelectedIndicator(ind);
    }
    if (perIdParam) setForm(f => ({ ...f, reportingPeriodId: parseInt(perIdParam) }));
  }, [searchParams, indicators]);

  // Select indicator
  const handleSelectIndicator = (ind: IndicatorResponse) => {
    setSelectedIndicator(ind);
    setForm(f => ({
      ...f,
      indicatorId: ind.id,
      dimensions: ind.dimensions.filter(d => d.isMandatory).map(d => ({ dimensionId: d.id })),
    }));
  };

  // Auto-save draft every 30 seconds
  const saveDraft = useCallback(async () => {
    if (form.indicatorId && form.reportingPeriodId) {
      try {
        await draftService.save({
          indicatorId: form.indicatorId,
          reportingPeriodId: form.reportingPeriodId,
          draftDataJson: JSON.stringify(form),
        });
      } catch { /* silent */ }
    }
  }, [form]);

  useEffect(() => {
    if (step === 2) {
      autoSaveTimer.current = setInterval(saveDraft, 30000);
    }
    return () => {
      if (autoSaveTimer.current) clearInterval(autoSaveTimer.current);
    };
  }, [step, saveDraft]);

  // Submit
  const handleSubmitEntry = async (asDraft: boolean) => {
    setLoading(true);
    setError("");
    try {
      const payload = {
        indicatorId: form.indicatorId,
        reportingPeriodId: form.reportingPeriodId,
        valueNumeric: form.valueNumeric ? parseFloat(form.valueNumeric) : null,
        valueText: form.valueText || null,
        notes: form.notes || null,
        source: form.source || null,
        dimensions: form.dimensions.map(d => ({
          dimensionId: d.dimensionId,
          dimensionValueId: d.dimensionValueId || null,
          valueNumeric: d.valueNumeric ? parseFloat(d.valueNumeric) : null,
        })),
      };

      const res = await entryService.create(payload);
      if (!res.data.success) {
        setError(res.data.message || "حدث خطأ");
        return;
      }

      if (!asDraft && res.data.data) {
        await entryService.submit(res.data.data.id);
      }

      navigate("/tasks");
    } catch (err: unknown) {
      const e = err as { response?: { data?: { message?: string } } };
      setError(e.response?.data?.message || "حدث خطأ أثناء الحفظ");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <PageMeta title="إدخال مؤشر | نظام إدارة المؤشرات" />

      {/* Progress indicator */}
      <div className="flex items-center gap-2 mb-6">
        {[1, 2, 3].map((s) => (
          <div key={s} className="flex items-center gap-2">
            <div className={`w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium ${
              s === step ? "bg-brand-500 text-white" : s < step ? "bg-success-500 text-white" : "bg-gray-200 text-gray-500 dark:bg-gray-700"
            }`}>
              {s < step ? "✓" : s}
            </div>
            <span className={`text-sm ${s === step ? "text-brand-600 font-medium" : "text-gray-400"}`}>
              {s === 1 ? "اختيار المؤشر" : s === 2 ? "إدخال القيمة" : "مراجعة وإرسال"}
            </span>
            {s < 3 && <div className="w-8 h-px bg-gray-300 dark:bg-gray-600" />}
          </div>
        ))}
      </div>

      {error && (
        <div className="p-3 mb-4 text-sm text-error-700 bg-error-50 rounded-lg border border-error-200">{error}</div>
      )}

      {/* Step 1: Indicator Selection */}
      {step === 1 && (
        <div className="rounded-2xl border border-gray-200 bg-white p-6 dark:border-gray-800 dark:bg-white/[0.03]">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-4">الخطوة 1: اختيار المؤشر والفترة</h3>

          <div className="space-y-4 max-w-xl">
            <div>
              <Label>المؤشر <span className="text-error-500">*</span></Label>
              <SearchableSelect
                options={indicators.map(i => ({ value: i.id.toString(), label: `${i.nameAr} (${i.code})` }))}
                value={form.indicatorId ? form.indicatorId.toString() : ""}
                onChange={(val) => {
                  const ind = indicators.find(i => i.id === parseInt(val));
                  if (ind) handleSelectIndicator(ind);
                }}
                placeholder="اختر المؤشر"
                searchPlaceholder="بحث بالاسم أو الرمز..."
              />
            </div>

            {selectedIndicator && (
              <div className="p-4 bg-gray-50 dark:bg-gray-800 rounded-lg text-sm space-y-2">
                <p><span className="text-gray-500">التعريف:</span> {selectedIndicator.definitionAr}</p>
                <p><span className="text-gray-500">آلية الاحتساب:</span> {selectedIndicator.calculationMethodAr}</p>
                <p><span className="text-gray-500">وحدة القياس:</span> {selectedIndicator.unitAr}</p>
              </div>
            )}

            <div>
              <Label>فترة الإبلاغ <span className="text-error-500">*</span></Label>
              <SearchableSelect
                options={periods.map(p => ({ value: p.id.toString(), label: p.displayNameAr }))}
                value={form.reportingPeriodId ? form.reportingPeriodId.toString() : ""}
                onChange={(val) => setForm(f => ({ ...f, reportingPeriodId: parseInt(val) || 0 }))}
                placeholder="اختر الفترة"
                searchPlaceholder="بحث عن الفترة..."
              />
            </div>

            <div className="pt-4">
              <Button size="sm" onClick={() => setStep(2)} disabled={!form.indicatorId || !form.reportingPeriodId}>
                التالي ←
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Step 2: Value Entry */}
      {step === 2 && selectedIndicator && (
        <div className="rounded-2xl border border-gray-200 bg-white p-6 dark:border-gray-800 dark:bg-white/[0.03]">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-4">الخطوة 2: إدخال القيمة</h3>

          <div className="space-y-4 max-w-xl">
            <div>
              <Label>القيمة الرقمية</Label>
              <Input type="number" value={form.valueNumeric} onChange={(e) => setForm(f => ({ ...f, valueNumeric: e.target.value }))}
                placeholder={`أدخل القيمة (${selectedIndicator.unitAr})`} />
              <p className="text-xs text-gray-400 mt-1">وحدة القياس: {selectedIndicator.unitAr}</p>
            </div>

            <div>
              <Label>القيمة النصية (اختياري)</Label>
              <Input value={form.valueText} onChange={(e) => setForm(f => ({ ...f, valueText: e.target.value }))}
                placeholder="أدخل قيمة نصية إن وُجدت" />
            </div>

            {/* Dimension values */}
            {selectedIndicator.dimensions.length > 0 && (
              <div>
                <h4 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">الأبعاد</h4>
                {selectedIndicator.dimensions.map((dim) => (
                  <div key={dim.id} className="mb-3">
                    <Label>{dim.dimensionNameAr} {dim.isMandatory && <span className="text-error-500">*</span>}</Label>
                    {dim.values.length > 0 ? (
                      <SearchableSelect
                        options={dim.values.filter(v => v.isActive).map(v => ({ value: v.id.toString(), label: v.valueAr }))}
                        value=""
                        onChange={(val) => {
                          setForm(f => ({
                            ...f,
                            dimensions: f.dimensions.map(d =>
                              d.dimensionId === dim.id ? { ...d, dimensionValueId: parseInt(val) } : d
                            ).concat(f.dimensions.find(d => d.dimensionId === dim.id) ? [] : [{ dimensionId: dim.id, dimensionValueId: parseInt(val) }])
                          }));
                        }}
                        placeholder="اختر القيمة"
                        searchPlaceholder="بحث..."
                      />
                    ) : (
                      <Input type="number" placeholder="أدخل القيمة الرقمية"
                        onChange={(e) => {
                          setForm(f => ({
                            ...f,
                            dimensions: f.dimensions.map(d =>
                              d.dimensionId === dim.id ? { ...d, valueNumeric: e.target.value } : d
                            ).concat(f.dimensions.find(d => d.dimensionId === dim.id) ? [] : [{ dimensionId: dim.id, valueNumeric: e.target.value }])
                          }));
                        }} />
                    )}
                  </div>
                ))}
              </div>
            )}

            <div>
              <Label>ملاحظات</Label>
              <textarea value={form.notes} onChange={(e) => setForm(f => ({ ...f, notes: e.target.value }))}
                rows={3} placeholder="أضف ملاحظات (اختياري)"
                className="w-full text-sm border border-gray-200 rounded-lg px-3 py-2.5 dark:border-gray-700 dark:bg-gray-800 dark:text-white" />
            </div>

            <div>
              <Label>المصدر</Label>
              <Input value={form.source} onChange={(e) => setForm(f => ({ ...f, source: e.target.value }))}
                placeholder="مصدر البيانات (اختياري)" />
            </div>

            <p className="text-xs text-gray-400">يتم حفظ المسودة تلقائياً كل 30 ثانية</p>

            <div className="flex gap-3 pt-4 justify-start">
              <Button size="sm" variant="outline" onClick={() => setStep(1)}>→ السابق</Button>
              <Button size="sm" onClick={() => setStep(3)}>التالي ←</Button>
            </div>
          </div>
        </div>
      )}

      {/* Step 3: Review and Submit */}
      {step === 3 && selectedIndicator && (
        <div className="rounded-2xl border border-gray-200 bg-white p-6 dark:border-gray-800 dark:bg-white/[0.03]">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-4">الخطوة 3: مراجعة وإرسال</h3>

          <div className="max-w-xl space-y-4">
            <div className="p-4 bg-gray-50 dark:bg-gray-800 rounded-lg space-y-2 text-sm">
              <ReviewRow label="المؤشر" value={selectedIndicator.nameAr} />
              <ReviewRow label="الرمز" value={selectedIndicator.code} />
              <ReviewRow label="الفترة" value={periods.find(p => p.id === form.reportingPeriodId)?.displayNameAr || "—"} />
              <ReviewRow label="القيمة الرقمية" value={form.valueNumeric || "—"} />
              <ReviewRow label="القيمة النصية" value={form.valueText || "—"} />
              <ReviewRow label="وحدة القياس" value={selectedIndicator.unitAr} />
              <ReviewRow label="الملاحظات" value={form.notes || "—"} />
              <ReviewRow label="المصدر" value={form.source || "—"} />
              {form.dimensions.length > 0 && (
                <div>
                  <span className="text-gray-500">الأبعاد:</span>
                  <span className="mr-2">{form.dimensions.length} بُعد</span>
                </div>
              )}
            </div>

            <div className="flex gap-3 pt-4 justify-start">
              <Button size="sm" variant="outline" onClick={() => setStep(2)}>→ السابق</Button>
              <Button size="sm" variant="outline" onClick={() => handleSubmitEntry(true)} disabled={loading}>
                {loading ? "جارٍ الحفظ..." : "حفظ كمسودة"}
              </Button>
              <Button size="sm" onClick={() => handleSubmitEntry(false)} disabled={loading}>
                {loading ? "جارٍ الإرسال..." : "إرسال للمراجعة"}
              </Button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

function ReviewRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between">
      <span className="text-gray-500">{label}:</span>
      <span className="text-gray-800 dark:text-white/90 font-medium">{value}</span>
    </div>
  );
}
