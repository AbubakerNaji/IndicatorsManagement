import { useSelector } from "react-redux";
import { Navigate } from "react-router";
import type { RootState } from "../store";

/**
 * Routes users to their role-specific landing page:
 * - Super_Admin / Ministry_Admin → Ministry Dashboard
 * - Entity_Admin → Tasks (entity dashboard)
 * - Data_Entry_User → Tasks
 * - Reviewer → Review Queue
 * - Auditor → Audit Console (placeholder)
 */
export default function RoleLandingPage() {
  const { user } = useSelector((state: RootState) => state.auth);

  if (!user) return <Navigate to="/signin" replace />;

  switch (user.role) {
    case "Super_Admin":
    case "Ministry_Admin":
      return <Navigate to="/dashboard/ministry" replace />;
    case "Entity_Admin":
    case "Data_Entry_User":
      return <Navigate to="/tasks" replace />;
    case "Reviewer":
      return <Navigate to="/review" replace />;
    case "Auditor":
      return <Navigate to="/audit" replace />;
    case "Viewer":
      return <Navigate to="/viewer/dashboard" replace />;
    default:
      return <Navigate to="/tasks" replace />;
  }
}
