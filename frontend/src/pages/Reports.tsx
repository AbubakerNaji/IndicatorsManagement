import { useState } from "react";
import PageMeta from "../components/common/PageMeta";
import PageBreadCrumb from "../components/common/PageBreadCrumb";
import api from "../services/api";
import type { ApiResponse } from "../services/authService";
import Button from "../components/ui/button/Button";

type ReportType = "submission-rate" | "compliance" | "historical";

interface ReportRow {
  [key: string]: string | number;
}

const REPORT_TYPES: { value: ReportType; label: string; description: string }[] = [
  { value: "submission-rate", label: "نسب الالتزام", description: "نسبة تسليم المؤشرات حسب الجهة أو المؤشر" },
  { value: "compliance", label: "تقرير الامتثال", description: "حالة التسليم والمتأخرات" },
  { value: "historical", label: "مقارنة تاريخية", description: "مقارنة القيم بين الفترة الحالية والسابقة" },
];

export default function Reports() {
  const [selectedType, setSelectedType] = useState<ReportType>("submission-rate");
  const [data, setData] = useState<ReportRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [generated, setGenerated] = useState(false);

  const generateReport = async () => {
    setLoading(true);
    try {
      let endpoint = "/dashboard/ministry";
      if (selectedType === "compliance") endpoint = "/dashboard/ministry";

      const res = await api.get<ApiResponse<Record<string, unknown>>>(endpoint);
      const d = res.data.data;

      if (selectedType === "submission-rate" && d) {
        const entities = (d.submissionsByEntity as Array<Record<string, unknown>>) || [];
        setData(entities.map((e) => ({
          الجهة: e.entityNameAr as string,
          "إجمالي الالتزامات": e.totalObligations as number,
          "المُسلّم": e.submittedCount as number,
          "نسبة الالتزام %": e.submissionRate as number,
        })));
      } else if (selectedType === "compliance" && d) {
        setData([{
          "إجمالي المؤشرات": d.totalIndicators as number,
          "إجمالي الجهات": d.totalEntities as number,
          "إجمالي الإدخالات": d.totalSubmissions as number,
          "المتأخرات": d.overdueCount as number,
        }]);
      } else {
        setData([{ ملاحظة: "هذا التقرير متاح في المرحلة الثانية" } as unknown as ReportRow]);
      }
      setGenerated(true);
    } finally {
      setLoading(false);
    }
  };

  const exportCSV = () => {
    if (data.length === 0) return;
    const headers = Object.keys(data[0]);
    const csv = [
      headers.join(","),
      ...data.map(row => headers.map(h => `"${row[h]}"`).join(","))
    ].join("\n");

    const blob = new Blob(["\uFEFF" + csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `report-${selectedType}-${new Date().toISOString().split("T")[0]}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <>
      <PageMeta title="التقارير | نظام إدارة المؤشرات" />
      <PageBreadCrumb pageTitle="التقارير" />

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6">
        {/* Report selection panel */}
        <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03]">
          <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-4">نوع التقرير</h3>
          <div className="space-y-2">
            {REPORT_TYPES.map((rt) => (
              <button key={rt.value} onClick={() => { setSelectedType(rt.value); setGenerated(false); }}
                className={`w-full text-right p-3 rounded-lg border transition-colors ${
                  selectedType === rt.value
                    ? "border-brand-500 bg-brand-50 dark:bg-brand-500/10"
                    : "border-gray-200 hover:border-gray-300 dark:border-gray-700"
                }`}>
                <p className="text-sm font-medium text-gray-800 dark:text-white/90">{rt.label}</p>
                <p className="text-xs text-gray-500 mt-1">{rt.description}</p>
              </button>
            ))}
          </div>

          <div className="mt-4">
            <Button size="sm" className="w-full" onClick={generateReport} disabled={loading}>
              {loading ? "جارٍ الإنشاء..." : "إنشاء التقرير"}
            </Button>
          </div>
        </div>

        {/* Report preview */}
        <div className="lg:col-span-3 rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03]">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90">
              {REPORT_TYPES.find(r => r.value === selectedType)?.label}
            </h3>
            {generated && data.length > 0 && (
              <Button size="sm" variant="outline" onClick={exportCSV}>تصدير CSV</Button>
            )}
          </div>

          {!generated ? (
            <div className="text-center py-16 text-gray-400">
              <p>اختر نوع التقرير واضغط "إنشاء التقرير"</p>
            </div>
          ) : data.length === 0 ? (
            <p className="text-gray-400 text-center py-8">لا توجد بيانات</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full table-auto text-sm">
                <thead>
                  <tr className="border-b border-gray-200 dark:border-gray-700">
                    {Object.keys(data[0]).map((header) => (
                      <th key={header} className="px-4 py-3 text-right font-medium text-gray-500">{header}</th>
                    ))}
                  </tr>
                </thead>
                <tbody>
                  {data.map((row, i) => (
                    <tr key={i} className="border-b border-gray-100 dark:border-gray-800">
                      {Object.values(row).map((val, j) => (
                        <td key={j} className="px-4 py-3 text-gray-800 dark:text-white/90">{val}</td>
                      ))}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </>
  );
}
