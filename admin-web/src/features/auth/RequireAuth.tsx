import { Box, CircularProgress } from "@mui/material";
import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "./AuthContext";

export function RequireAuth() {
  const auth = useAuth();
  const location = useLocation();

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

  if (auth.status !== "authenticated") {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <Outlet />;
}
