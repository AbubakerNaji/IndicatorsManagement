import { BrowserRouter as Router, Routes, Route } from "react-router";
import SignIn from "./pages/AuthPages/SignIn";
import NotFound from "./pages/OtherPage/NotFound";
import Unauthorized from "./pages/OtherPage/Unauthorized";
import UserProfiles from "./pages/UserProfiles";
import AppLayout from "./layout/AppLayout";
import { ScrollToTop } from "./components/common/ScrollToTop";
import ErrorBoundary from "./components/common/ErrorBoundary";
import ProtectedRoute from "./components/auth/ProtectedRoute";
import RoleLandingPage from "./pages/RoleLandingPage";
import UserManagement from "./pages/UserManagement";
import IndicatorManagement from "./pages/IndicatorManagement";
import EntityManagement from "./pages/EntityManagement";
import ReportingPeriods from "./pages/ReportingPeriods";
import AssignmentManagement from "./pages/AssignmentManagement";
import TaskDashboard from "./pages/TaskDashboard";
import ReviewQueue from "./pages/ReviewQueue";
import MinistryDashboard from "./pages/MinistryDashboard";
import NotificationsCenter from "./pages/NotificationsCenter";
import AuditConsole from "./pages/AuditConsole";
import SystemConfig from "./pages/SystemConfig";
import EntryWizard from "./pages/EntryWizard";
import Reports from "./pages/Reports";
import PublicationManagement from "./pages/PublicationManagement";
import ViewerDashboard from "./pages/ViewerDashboard";
import DraftRecoveryModal from "./components/auth/DraftRecoveryModal";

export default function App() {
  return (
    <ErrorBoundary>
      <Router>
        <ScrollToTop />
        <Routes>
          {/* Auth - Public */}
          <Route path="/signin" element={<SignIn />} />
          <Route path="/unauthorized" element={<Unauthorized />} />

        {/* Protected Dashboard Layout */}
        <Route element={
          <ProtectedRoute>
            <>
              <DraftRecoveryModal />
              <AppLayout />
            </>
          </ProtectedRoute>
        }>
          {/* Role-based landing page */}
          <Route index path="/" element={<RoleLandingPage />} />
          <Route path="/profile" element={<UserProfiles />} />

          {/* Dashboards */}
          <Route path="/dashboard/ministry" element={
            <ProtectedRoute allowedRoles={["Super_Admin", "Ministry_Admin"]}><MinistryDashboard /></ProtectedRoute>
          } />
          <Route path="/tasks" element={<TaskDashboard />} />
          <Route path="/entries/new" element={
            <ProtectedRoute allowedRoles={["Super_Admin", "Entity_Admin", "Data_Entry_User"]}><EntryWizard /></ProtectedRoute>
          } />
          <Route path="/review" element={
            <ProtectedRoute allowedRoles={["Super_Admin", "Ministry_Admin", "Entity_Admin", "Reviewer"]}><ReviewQueue /></ProtectedRoute>
          } />

          {/* Master Data */}
          <Route path="/indicators" element={
            <ProtectedRoute allowedRoles={["Super_Admin", "Ministry_Admin"]}><IndicatorManagement /></ProtectedRoute>
          } />
          <Route path="/entities" element={
            <ProtectedRoute allowedRoles={["Super_Admin"]}><EntityManagement /></ProtectedRoute>
          } />
          <Route path="/reporting-periods" element={
            <ProtectedRoute allowedRoles={["Super_Admin"]}><ReportingPeriods /></ProtectedRoute>
          } />
          <Route path="/assignments" element={
            <ProtectedRoute allowedRoles={["Super_Admin", "Ministry_Admin"]}><AssignmentManagement /></ProtectedRoute>
          } />

          {/* Users */}
          <Route path="/users" element={
            <ProtectedRoute allowedRoles={["Super_Admin", "Ministry_Admin", "Entity_Admin"]}><UserManagement /></ProtectedRoute>
          } />

          {/* Reports */}
          <Route path="/reports" element={<Reports />} />

          {/* V2.1: Publication Control */}
          <Route path="/publication" element={
            <ProtectedRoute allowedRoles={["Super_Admin", "Ministry_Admin"]}><PublicationManagement /></ProtectedRoute>
          } />
          <Route path="/viewer/dashboard" element={
            <ProtectedRoute allowedRoles={["Viewer"]}><ViewerDashboard /></ProtectedRoute>
          } />

          {/* Notifications */}
          <Route path="/notifications" element={<NotificationsCenter />} />

          {/* Audit */}
          <Route path="/audit" element={
            <ProtectedRoute allowedRoles={["Super_Admin", "Auditor"]}><AuditConsole /></ProtectedRoute>
          } />

          {/* Configuration */}
          <Route path="/config" element={
            <ProtectedRoute allowedRoles={["Super_Admin", "Ministry_Admin"]}><SystemConfig /></ProtectedRoute>
          } />
        </Route>

        {/* Fallback */}
        <Route path="*" element={<NotFound />} />
      </Routes>
    </Router>
    </ErrorBoundary>
  );
}
