interface WorkflowBadgeProps {
  state: string;
}

const WORKFLOW_CONFIG: Record<string, { label: string; bg: string; text: string }> = {
  Draft: { label: "مسودة", bg: "bg-gray-100 dark:bg-gray-700", text: "text-gray-600 dark:text-gray-300" },
  Under_Review: { label: "قيد المراجعة", bg: "bg-blue-light-50 dark:bg-blue-light-900/20", text: "text-blue-light-600 dark:text-blue-light-400" },
  Approved_By_Entity: { label: "معتمد من الجهة", bg: "bg-success-50 dark:bg-success-900/20", text: "text-success-500" },
  Final_Approved: { label: "معتمد نهائياً", bg: "bg-success-100 dark:bg-success-900/30", text: "text-success-700 dark:text-success-400" },
  Rejected: { label: "مرفوض", bg: "bg-error-50 dark:bg-error-900/20", text: "text-error-600 dark:text-error-400" },
  Returned_For_Modification: { label: "مُعاد للتعديل", bg: "bg-orange-50 dark:bg-orange-900/20", text: "text-orange-600 dark:text-orange-400" },
};

const OBLIGATION_CONFIG: Record<string, { label: string; bg: string; text: string }> = {
  Not_Started: { label: "لم يبدأ", bg: "bg-gray-100", text: "text-gray-500" },
  In_Progress: { label: "قيد التنفيذ", bg: "bg-blue-light-50", text: "text-blue-light-600" },
  Submitted: { label: "تم الإرسال", bg: "bg-success-50", text: "text-success-500" },
  Overdue: { label: "متأخر", bg: "bg-error-50", text: "text-error-600" },
  Approved: { label: "معتمد", bg: "bg-success-100", text: "text-success-700" },
};

export default function WorkflowBadge({ state }: WorkflowBadgeProps) {
  const config = WORKFLOW_CONFIG[state] || OBLIGATION_CONFIG[state] || {
    label: state, bg: "bg-gray-100", text: "text-gray-500"
  };

  return (
    <span className={`inline-flex items-center px-2.5 py-1 text-xs font-medium rounded-full ${config.bg} ${config.text}`}>
      {config.label}
    </span>
  );
}
