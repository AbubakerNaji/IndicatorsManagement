import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import { useSelector } from "react-redux";
import { draftService, type DraftRecoveryItem } from "../../services/dashboardService";
import { Modal } from "../ui/modal";
import Button from "../ui/button/Button";
import type { RootState } from "../../store";

export default function DraftRecoveryModal() {
  const { isAuthenticated } = useSelector((state: RootState) => state.auth);
  const [drafts, setDrafts] = useState<DraftRecoveryItem[]>([]);
  const [isOpen, setIsOpen] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    if (!isAuthenticated) return;
    const check = async () => {
      try {
        const res = await draftService.getRecoverable();
        const items = res.data.data || [];
        if (items.length > 0) {
          setDrafts(items);
          setIsOpen(true);
        }
      } catch { /* ignore */ }
    };
    check();
  }, [isAuthenticated]);

  const handleRestore = (draft: DraftRecoveryItem) => {
    setIsOpen(false);
    navigate(`/entries/new?indicatorId=${draft.indicatorId}&periodId=${draft.reportingPeriodId}`);
  };

  const handleDiscard = async (id: number) => {
    await draftService.discard(id);
    setDrafts(d => d.filter(dr => dr.id !== id));
    if (drafts.length <= 1) setIsOpen(false);
  };

  if (!isOpen || drafts.length === 0) return null;

  return (
    <Modal isOpen={isOpen} onClose={() => setIsOpen(false)} className="max-w-lg">
      <div className="p-6">
        <h3 className="text-lg font-semibold text-gray-800 dark:text-white/90 mb-2">مسودات غير محفوظة</h3>
        <p className="text-sm text-gray-500 mb-4">تم العثور على مسودات سابقة. هل تريد استعادتها؟</p>

        <div className="space-y-3 mb-4">
          {drafts.map((draft) => (
            <div key={draft.id} className="p-3 border border-gray-200 dark:border-gray-700 rounded-lg">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-800 dark:text-white/90">{draft.indicatorNameAr}</p>
                  <p className="text-xs text-gray-500">{draft.periodDisplayNameAr}</p>
                  <p className="text-xs text-gray-400">آخر حفظ: {new Date(draft.lastSavedAt).toLocaleString("ar-LY")}</p>
                </div>
                <div className="flex gap-2">
                  <Button size="sm" onClick={() => handleRestore(draft)}>استعادة</Button>
                  <Button size="sm" variant="outline" onClick={() => handleDiscard(draft.id)}>حذف</Button>
                </div>
              </div>
            </div>
          ))}
        </div>

        <button onClick={() => setIsOpen(false)} className="text-sm text-gray-500 hover:text-gray-700">
          تخطي
        </button>
      </div>
    </Modal>
  );
}
