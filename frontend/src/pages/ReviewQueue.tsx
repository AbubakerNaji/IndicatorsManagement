import { useEffect, useState } from "react";
import PageMeta from "../components/common/PageMeta";
import PageBreadCrumb from "../components/common/PageBreadCrumb";
import { entryService, type IndicatorEntryResponse } from "../services/entryService";
import WorkflowBadge from "../components/ui/badge/WorkflowBadge";
import Button from "../components/ui/button/Button";

export default function ReviewQueue() {
  const [entries, setEntries] = useState<IndicatorEntryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<IndicatorEntryResponse | null>(null);
  const [actionNotes, setActionNotes] = useState("");

  const loadEntries = async () => {
    setLoading(true);
    try {
      const res = await entryService.getAll({ pageSize: 100 });
      const items = res.data.data?.items || [];
      // Show entries pending review (Under_Review and Approved_By_Entity)
      setEntries(items.filter(e => e.workflowState === "Under_Review" || e.workflowState === "Approved_By_Entity"));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { loadEntries(); }, []);

  const handleAction = async (action: "approve-entity" | "approve-ministry" | "reject" | "return") => {
    if (!selected) return;
    if ((action === "reject" || action === "return") && !actionNotes.trim()) {
      alert("يجب إدخال ملاحظات");
      return;
    }

    try {
      if (action === "approve-entity") await entryService.approveEntity(selected.id, actionNotes);
      else if (action === "approve-ministry") await entryService.approveMinistry(selected.id, actionNotes);
      else if (action === "reject") await entryService.reject(selected.id, actionNotes);
      else if (action === "return") await entryService.returnEntry(selected.id, actionNotes);

      setSelected(null);
      setActionNotes("");
      loadEntries();
    } catch {
      alert("حدث خطأ أثناء تنفيذ الإجراء");
    }
  };

  return (
    <>
      <PageMeta title="قائمة المراجعة | نظام إدارة المؤشرات" />
      <PageBreadCrumb pageTitle="قائمة المراجعة" />

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Entries list */}
        <div className="lg:col-span-2 rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03]">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-4">
            الإدخالات المعلّقة ({entries.length})
          </h3>

          {loading ? (
            <p className="text-gray-500 text-center py-8">جارٍ التحميل...</p>
          ) : entries.length === 0 ? (
            <p className="text-gray-400 text-center py-8">لا توجد إدخالات بانتظار المراجعة</p>
          ) : (
            <div className="space-y-3">
              {entries.map((entry) => (
                <div
                  key={entry.id}
                  onClick={() => { setSelected(entry); setActionNotes(""); }}
                  className={`p-4 rounded-xl border cursor-pointer transition-colors ${
                    selected?.id === entry.id
                      ? "border-brand-500 bg-brand-50 dark:bg-brand-500/10"
                      : "border-gray-200 hover:border-gray-300 dark:border-gray-700"
                  }`}
                >
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-sm font-medium text-gray-800 dark:text-white/90">{entry.indicatorNameAr}</span>
                    <WorkflowBadge state={entry.workflowState} />
                  </div>
                  <div className="flex items-center gap-4 text-xs text-gray-500">
                    <span>{entry.entityNameAr}</span>
                    <span>{entry.periodDisplayNameAr}</span>
                    <span className="font-mono">{entry.valueNumeric ?? entry.valueText ?? "—"}</span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Detail panel */}
        <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03]">
          {selected ? (
            <div>
              <h4 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-4">تفاصيل الإدخال</h4>

              <div className="space-y-3 text-sm mb-6">
                <div className="flex justify-between">
                  <span className="text-gray-500">المؤشر:</span>
                  <span className="text-gray-800 dark:text-white/90">{selected.indicatorNameAr}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">الجهة:</span>
                  <span>{selected.entityNameAr}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">الفترة:</span>
                  <span>{selected.periodDisplayNameAr}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">القيمة:</span>
                  <span className="font-semibold text-gray-800 dark:text-white">{selected.valueNumeric ?? selected.valueText ?? "—"} {selected.unitSnapshot}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-gray-500">المُدخل:</span>
                  <span>{selected.enteredByName}</span>
                </div>
                {selected.notes && (
                  <div>
                    <span className="text-gray-500 block mb-1">ملاحظات:</span>
                    <p className="text-gray-700 dark:text-gray-300 bg-gray-50 dark:bg-gray-800 p-2 rounded">{selected.notes}</p>
                  </div>
                )}
                {selected.attachments.length > 0 && (
                  <div>
                    <span className="text-gray-500 block mb-1">المرفقات ({selected.attachments.length}):</span>
                    {selected.attachments.map(a => (
                      <p key={a.id} className="text-xs text-brand-500">{a.fileName}</p>
                    ))}
                  </div>
                )}
              </div>

              <div className="mb-4">
                <label className="text-sm text-gray-500 block mb-1">ملاحظات الإجراء</label>
                <textarea
                  value={actionNotes}
                  onChange={(e) => setActionNotes(e.target.value)}
                  rows={3}
                  className="w-full text-sm border border-gray-200 rounded-lg p-2 dark:border-gray-700 dark:bg-gray-800 dark:text-white"
                  placeholder="أدخل ملاحظات (إلزامي للرفض والإعادة)"
                />
              </div>

              <div className="grid grid-cols-2 gap-2">
                {selected.workflowState === "Under_Review" && (
                  <Button size="sm" onClick={() => handleAction("approve-entity")} className="bg-success-500 hover:bg-success-600">
                    اعتماد الجهة
                  </Button>
                )}
                {selected.workflowState === "Approved_By_Entity" && (
                  <Button size="sm" onClick={() => handleAction("approve-ministry")} className="bg-success-700 hover:bg-success-800">
                    اعتماد نهائي
                  </Button>
                )}
                <Button size="sm" variant="outline" onClick={() => handleAction("return")} className="text-orange-600 border-orange-300">
                  إعادة للتعديل
                </Button>
                <Button size="sm" variant="outline" onClick={() => handleAction("reject")} className="text-error-600 border-error-300 col-span-2">
                  رفض
                </Button>
              </div>
            </div>
          ) : (
            <div className="text-center py-12 text-gray-400">
              <p>اختر إدخالاً من القائمة لعرض التفاصيل</p>
            </div>
          )}
        </div>
      </div>
    </>
  );
}
