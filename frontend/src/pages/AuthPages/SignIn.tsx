import { useEffect } from "react";
import PageMeta from "../../components/common/PageMeta";
import AuthLayout from "./AuthPageLayout";
import SignInForm from "../../components/auth/SignInForm";

export default function SignIn() {
  useEffect(() => {
    // Drop any stale token so a bad session can't loop us back here.
    localStorage.removeItem("token");
    localStorage.removeItem("user");
  }, []);

  return (
    <>
      <PageMeta
        title="تسجيل الدخول | نظام إدارة المؤشرات"
        description="صفحة تسجيل الدخول لنظام إدارة المؤشرات - وزارة الاقتصاد والتجارة"
      />
      <AuthLayout>
        <SignInForm />
      </AuthLayout>
    </>
  );
}
