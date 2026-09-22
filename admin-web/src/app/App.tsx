import { Box, CircularProgress, CssBaseline } from "@mui/material";
import { ThemeProvider, createTheme } from "@mui/material/styles";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AdminLayout } from "../components/Layout/AdminLayout";
import { AuthProvider, useAuth } from "../features/auth/AuthContext";
import { LoginPage } from "../features/auth/LoginPage";
import { RequireAuth } from "../features/auth/RequireAuth";
import { AboutPage } from "../features/pages/AboutPage";
import { ProjectsPage } from "../features/projects/ProjectsPage";

const adminBasePath = (import.meta.env.VITE_ADMIN_BASE_PATH || "/admin/").replace(/\/$/, "");

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1
    }
  }
});

const theme = createTheme({
  palette: {
    background: {
      default: "#f6f7f9"
    },
    primary: {
      main: "#2563eb"
    }
  },
  shape: {
    borderRadius: 8
  },
  typography: {
    fontFamily:
      'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif'
  }
});

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <CssBaseline />
        <AuthProvider>
          <BrowserRouter basename={adminBasePath}>
            <Routes>
              <Route element={<RootRedirect />} path="/" />
              <Route element={<LoginPage />} path="/login" />
              <Route element={<RequireAuth />}>
                <Route element={<AdminLayout />}>
                  <Route
                    element={<AboutPage />}
                    path="/about"
                  />
                  <Route
                    element={<ProjectsPage category="AI" title="Проекты с ИИ" />}
                    path="/ai-projects"
                  />
                  <Route
                    element={<ProjectsPage category="OTHER" title="Другие проекты" />}
                    path="/other-projects"
                  />
                </Route>
              </Route>
              <Route element={<Navigate replace to="/" />} path="*" />
            </Routes>
          </BrowserRouter>
        </AuthProvider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

function RootRedirect() {
  const auth = useAuth();

  if (auth.status === "checking") {
    return (
      <Box
        sx={{
          alignItems: "center",
          display: "flex",
          justifyContent: "center",
          minHeight: "100vh"
        }}
      >
        <CircularProgress aria-label="Проверка авторизации" />
      </Box>
    );
  }

  if (auth.status === "authenticated") {
    return <Navigate replace to="/about" />;
  }

  return <Navigate replace to="/login" />;
}
