import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Paper,
  Stack,
  TextField,
  Typography
} from "@mui/material";
import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { Controller, useForm } from "react-hook-form";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { ApiError } from "../../shared/api/client";
import type { LoginRequest } from "../../shared/types/auth";
import { useAuth } from "./AuthContext";

export function LoginPage() {
  const auth = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const { control, handleSubmit } = useForm<LoginRequest>({
    defaultValues: {
      login: "",
      password: ""
    }
  });

  const mutation = useMutation({
    mutationFn: auth.login,
    onSuccess: () => {
      navigate("/about", { replace: true });
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 401) {
        setErrorMessage("Неверный логин или пароль.");
        return;
      }

      setErrorMessage(error instanceof Error ? error.message : "Не удалось войти.");
    }
  });

  if (auth.status === "authenticated") {
    const from = (location.state as { from?: Location } | null)?.from?.pathname ?? "/about";
    return <Navigate to={from} replace />;
  }

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

  return (
    <Box
      sx={{
        alignItems: "center",
        backgroundColor: "background.default",
        display: "flex",
        minHeight: "100vh",
        py: 4
      }}
    >
      <Container maxWidth="xs">
        <Paper elevation={0} sx={{ border: 1, borderColor: "divider", p: { xs: 3, sm: 4 } }}>
          <Stack spacing={3}>
            <Box>
              <Typography component="h1" variant="h4">
                Вход в админку
              </Typography>
              <Typography color="text.secondary" sx={{ mt: 1 }} variant="body2">
                Используйте учетные данные администратора.
              </Typography>
            </Box>

            {errorMessage ? <Alert severity="error">{errorMessage}</Alert> : null}

            <Stack component="form" noValidate spacing={2.5} onSubmit={handleSubmit((values) => mutation.mutate(values))}>
              <Controller
                control={control}
                name="login"
                rules={{ required: "Введите логин." }}
                render={({ field, fieldState }) => (
                  <TextField
                    {...field}
                    autoComplete="username"
                    autoFocus
                    disabled={mutation.isPending}
                    error={Boolean(fieldState.error)}
                    fullWidth
                    helperText={fieldState.error?.message}
                    label="Логин"
                  />
                )}
              />

              <Controller
                control={control}
                name="password"
                rules={{ required: "Введите пароль." }}
                render={({ field, fieldState }) => (
                  <TextField
                    {...field}
                    autoComplete="current-password"
                    disabled={mutation.isPending}
                    error={Boolean(fieldState.error)}
                    fullWidth
                    helperText={fieldState.error?.message}
                    label="Пароль"
                    type="password"
                  />
                )}
              />

              <Button disabled={mutation.isPending} size="large" type="submit" variant="contained">
                {mutation.isPending ? <CircularProgress color="inherit" size={22} /> : "Войти"}
              </Button>
            </Stack>
          </Stack>
        </Paper>
      </Container>
    </Box>
  );
}
