import { useEffect, useState } from "react";
import PageMeta from "../components/common/PageMeta";
import PageBreadCrumb from "../components/common/PageBreadCrumb";
import api from "../services/api";
import type { ApiResponse } from "../services/authService";
import Button from "../components/ui/button/Button";
import Label from "../components/form/Label";
import Input from "../components/form/input/InputField";

interface ConfigEntry {
  key: string;
  label: string;
  unit: string;
}

const CONFIG_FIELDS: ConfigEntry[] = [
  { key: "NotificationThreshold_Days_7", label: "تنبيه قبل الموعد (أيام) - المستوى الأول", unit: "يوم" },
  { key: "NotificationThreshold_Days_3", label: "تنبيه قبل الموعد (أيام) - المستوى الثاني", unit: "يوم" },
  { key: "NotificationThreshold_Days_1", label: "تنبيه قبل الموعد (أيام) - المستوى الثالث", unit: "يوم" },
  { key: "DashboardRefreshInterval_Minutes", label: "فترة تحديث لوحة المتابعة", unit: "دقيقة" },
  { key: "FileUploadMaxSize_MB", label: "الحد الأقصى لحجم الملف المرفق", unit: "ميغابايت" },
  { key: "SessionTimeout_Minutes", label: "مهلة انتهاء الجلسة", unit: "دقيقة" },
];

export default function SystemConfig() {
  const [config, setConfig] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState<string | null>(null);

  useEffect(() => {
    const load = async () => {
      try {
        const res = await api.get<ApiResponse<Record<string, string>>>("/config");
        setConfig(res.data.data || {});
      } finally {
        setLoading(false);
      }
    };
    load();
  }, []);

  const handleSave = async (key: string) => {
    setSaving(key);
    try {
      await api.put("/config", { key, value: config[key] });
    } finally {
      setSaving(null);
    }
  };

  if (loading) return <p className="text-gray-500 text-center py-12">جارٍ التحميل...</p>;

  return (
    <>
      <PageMeta title="إعدادات النظام | نظام إدارة المؤشرات" />
      <PageBreadCrumb pageTitle="إعدادات النظام" />

      <div className="rounded-2xl border border-gray-200 bg-white p-5 dark:border-gray-800 dark:bg-white/[0.03] lg:p-6">
        <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-6">الإعدادات العامة</h3>

        <div className="space-y-6 max-w-xl">
          {CONFIG_FIELDS.map((field) => (
            <div key={field.key} className="flex items-end gap-3">
              <div className="flex-1">
                <Label>{field.label}</Label>
                <div className="flex items-center gap-2">
                  <Input
                    type="number"
                    value={config[field.key] || ""}
                    onChange={(e) => setConfig({ ...config, [field.key]: e.target.value })}
                  />
                  <span className="text-sm text-gray-500 whitespace-nowrap">{field.unit}</span>
                </div>
              </div>
              <Button size="sm" onClick={() => handleSave(field.key)} disabled={saving === field.key}>
                {saving === field.key ? "جارٍ الحفظ..." : "حفظ"}
              </Button>
            </div>
          ))}
        </div>
      </div>
    </>
  );
}
