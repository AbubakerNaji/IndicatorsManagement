import { Link } from "react-router";

/**
 * F2 — dedicated Unauthorized page. `ProtectedRoute` redirects here when the current
 * user's role isn't in the route's allowlist. Previously this route fell through to
 * NotFound, which was confusing.
 */
export default function Unauthorized() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-gray-50 px-4 text-center dark:bg-gray-900">
      <div className="max-w-md">
        <h1 className="text-6xl font-bold text-red-600 dark:text-red-400">403</h1>
        <h2 className="mt-4 text-2xl font-semibold text-gray-900 dark:text-white">
          غير مصرح لك بالوصول
        </h2>
        <p className="mt-2 text-gray-600 dark:text-gray-400">
          دورك الحالي لا يخوّلك عرض هذه الصفحة. إذا كنت تعتقد أن هذا خطأ،
          يرجى التواصل مع مسؤول النظام.
        </p>
        <div className="mt-6 flex justify-center gap-3">
          <Link
            to="/"
            className="rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
          >
            العودة للرئيسية
          </Link>
          <Link
            to="/signin"
            className="rounded-md border border-gray-300 px-4 py-2 text-gray-700 hover:bg-gray-100 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-800"
          >
            تسجيل دخول بحساب آخر
          </Link>
        </div>
      </div>
    </div>
  );
}
