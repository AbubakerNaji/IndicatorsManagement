import { useState } from "react";
import { useNavigate } from "react-router";
import { z } from "zod";
import { useDispatch, useSelector } from "react-redux";
import { EyeCloseIcon, EyeIcon } from "../../icons";
import Label from "../form/Label";
import Input from "../form/input/InputField";
import Checkbox from "../form/input/Checkbox";
import Button from "../ui/button/Button";
import { login, clearError } from "../../store/authSlice";
import type { AppDispatch, RootState } from "../../store";

const loginSchema = z.object({
  userNameOrEmail: z.string().min(1, "اسم المستخدم أو البريد الإلكتروني مطلوب"),
  password: z.string().min(1, "كلمة المرور مطلوبة"),
});

export default function SignInForm() {
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(false);
  const [formData, setFormData] = useState({ userNameOrEmail: "", password: "" });
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const dispatch = useDispatch<AppDispatch>();
  const navigate = useNavigate();
  const { loading, error } = useSelector((state: RootState) => state.auth);

  const handleChange = (field: string) => (e: React.ChangeEvent<HTMLInputElement>) => {
    setFormData((prev) => ({ ...prev, [field]: e.target.value }));
    setFieldErrors((prev) => ({ ...prev, [field]: "" }));
  };

  const handleSubmitForm = async (e: React.FormEvent) => {
    e.preventDefault();
    dispatch(clearError());

    // Validate with Zod
    const result = loginSchema.safeParse(formData);
    if (!result.success) {
      const errs: Record<string, string> = {};
      result.error.issues.forEach((issue) => {
        if (issue.path[0]) errs[issue.path[0] as string] = issue.message;
      });
      setFieldErrors(errs);
      return;
    }

    const loginResult = await dispatch(login(result.data));
    if (login.fulfilled.match(loginResult)) {
      navigate("/");
    }
  };

  return (
    <div className="flex flex-col flex-1">
      <div className="flex flex-col justify-center flex-1 w-full max-w-md mx-auto">
        <div>
          <div className="mb-5 sm:mb-8">
            <h1 className="mb-2 font-semibold text-gray-800 text-title-sm dark:text-white/90 sm:text-title-md">
              تسجيل الدخول
            </h1>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              أدخل اسم المستخدم وكلمة المرور للدخول إلى النظام
            </p>
          </div>

          {error && (
            <div className="p-3 mb-4 text-sm text-error-700 bg-error-50 rounded-lg border border-error-200 dark:bg-error-900/20 dark:text-error-400 dark:border-error-800">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmitForm}>
            <div className="space-y-6">
              <div>
                <Label>
                  اسم المستخدم أو البريد الإلكتروني <span className="text-error-500">*</span>
                </Label>
                <Input
                  placeholder="أدخل اسم المستخدم أو البريد الإلكتروني"
                  value={formData.userNameOrEmail}
                  onChange={handleChange("userNameOrEmail")}
                  error={!!fieldErrors.userNameOrEmail}
                  hint={fieldErrors.userNameOrEmail}
                />
              </div>
              <div>
                <Label>
                  كلمة المرور <span className="text-error-500">*</span>
                </Label>
                <div className="relative">
                  <Input
                    type={showPassword ? "text" : "password"}
                    placeholder="أدخل كلمة المرور"
                    value={formData.password}
                    onChange={handleChange("password")}
                    error={!!fieldErrors.password}
                    hint={fieldErrors.password}
                  />
                  <span
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute z-30 -translate-y-1/2 cursor-pointer left-4 top-1/2"
                  >
                    {showPassword ? (
                      <EyeIcon className="fill-gray-500 dark:fill-gray-400 size-5" />
                    ) : (
                      <EyeCloseIcon className="fill-gray-500 dark:fill-gray-400 size-5" />
                    )}
                  </span>
                </div>
              </div>
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <Checkbox checked={rememberMe} onChange={setRememberMe} />
                  <span className="block font-normal text-gray-700 text-theme-sm dark:text-gray-400">
                    تذكرني
                  </span>
                </div>
              </div>
              <div>
                <Button className="w-full" size="sm" disabled={loading}>
                  {loading ? "جارٍ تسجيل الدخول..." : "تسجيل الدخول"}
                </Button>
              </div>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
