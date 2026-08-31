import { createSlice, createAsyncThunk } from "@reduxjs/toolkit";
import { authService, type UserInfo, type LoginRequest } from "../services/authService";

interface AuthState {
  user: UserInfo | null;
  token: string | null;
  isAuthenticated: boolean;
  loading: boolean;
  error: string | null;
}

// Load from localStorage
const storedToken = localStorage.getItem("token");
const storedUser = localStorage.getItem("user");

const initialState: AuthState = {
  user: storedUser ? JSON.parse(storedUser) : null,
  token: storedToken,
  isAuthenticated: !!storedToken,
  loading: false,
  error: null,
};

export const login = createAsyncThunk(
  "auth/login",
  async (credentials: LoginRequest, { rejectWithValue }) => {
    try {
      const response = await authService.login(credentials);
      const data = response.data;
      if (!data.success || !data.data) {
        return rejectWithValue(data.message || "فشل تسجيل الدخول");
      }
      // Persist
      localStorage.setItem("token", data.data.token);
      localStorage.setItem("user", JSON.stringify(data.data.user));
      return data.data;
    } catch (error: unknown) {
      const err = error as {
        response?: { status?: number; data?: { message?: string } };
        code?: string;
        message?: string;
      };
      const status = err.response?.status;
      if (status === 429) {
        return rejectWithValue(
          "لقد تجاوزت الحد المسموح لمحاولات الدخول. الرجاء الانتظار دقيقة والمحاولة مرة أخرى."
        );
      }
      if (status === 401) {
        return rejectWithValue(
          err.response?.data?.message || "اسم المستخدم أو كلمة المرور غير صحيحة"
        );
      }
      if (err.code === "ERR_NETWORK") {
        return rejectWithValue(
          "تعذر الاتصال بالخادم. تأكد من تشغيل الواجهة الخلفية على http://localhost:5117"
        );
      }
      return rejectWithValue(err.response?.data?.message || "فشل تسجيل الدخول");
    }
  }
);

export const logout = createAsyncThunk("auth/logout", async () => {
  try {
    await authService.logout();
  } finally {
    localStorage.removeItem("token");
    localStorage.removeItem("user");
  }
});

const authSlice = createSlice({
  name: "auth",
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(login.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(login.fulfilled, (state, action) => {
        state.loading = false;
        state.isAuthenticated = true;
        state.token = action.payload.token;
        state.user = action.payload.user;
      })
      .addCase(login.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      })
      .addCase(logout.fulfilled, (state) => {
        state.user = null;
        state.token = null;
        state.isAuthenticated = false;
      });
  },
});

export const { clearError } = authSlice.actions;
export default authSlice.reducer;
