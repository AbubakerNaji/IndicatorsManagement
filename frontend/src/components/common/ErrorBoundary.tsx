import { Component, type ErrorInfo, type ReactNode } from "react";

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

/**
 * F6 — root error boundary. Any uncaught render error in a child component is
 * captured here so the whole SPA doesn't reduce to a white page. Users see an
 * Arabic apology and a way to recover; developers see the error in the console.
 */
export default class ErrorBoundary extends Component<Props, State> {
  state: State = { hasError: false, error: null };

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error("Uncaught error:", error, info);
  }

  private handleReset = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (this.state.hasError) {
      return (
        <div
          dir="rtl"
          className="flex min-h-screen flex-col items-center justify-center bg-gray-50 px-4 text-center dark:bg-gray-900"
        >
          <div className="max-w-md">
            <h1 className="text-6xl font-bold text-red-600 dark:text-red-400">!</h1>
            <h2 className="mt-4 text-2xl font-semibold text-gray-900 dark:text-white">
              حدث خطأ غير متوقع
            </h2>
            <p className="mt-2 text-gray-600 dark:text-gray-400">
              نأسف لهذا الإزعاج. تم تسجيل الخطأ. يمكنك المحاولة مجدداً أو العودة
              إلى الصفحة الرئيسية.
            </p>
            {import.meta.env.DEV && this.state.error && (
              <pre className="mx-auto mt-4 max-w-full overflow-auto rounded bg-gray-100 p-3 text-start text-xs text-red-800 dark:bg-gray-800 dark:text-red-300">
                {this.state.error.message}
              </pre>
            )}
            <div className="mt-6 flex justify-center gap-3">
              <button
                onClick={this.handleReset}
                className="rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
              >
                المحاولة مجدداً
              </button>
              <a
                href="/"
                className="rounded-md border border-gray-300 px-4 py-2 text-gray-700 hover:bg-gray-100 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-800"
              >
                الصفحة الرئيسية
              </a>
            </div>
          </div>
        </div>
      );
    }
    return this.props.children;
  }
}
