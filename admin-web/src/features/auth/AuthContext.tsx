import { createContext, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { AUTH_UNAUTHORIZED_EVENT } from "../../shared/api/client";
import { getCurrentUser, loginAdmin } from "../../shared/api/auth";
import { tokenStorage } from "../../shared/api/tokenStorage";
import type { AuthUser, LoginRequest } from "../../shared/types/auth";

type AuthStatus = "checking" | "authenticated" | "unauthenticated";

interface AuthContextValue {
  login: (request: LoginRequest) => Promise<void>;
  logout: () => void;
  status: AuthStatus;
  token: string | null;
  user: AuthUser | null;
}

const AuthContext = createContext<AuthContextValue | null>(null);

interface AuthProviderProps {
  children: React.ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [token, setToken] = useState<string | null>(() => tokenStorage.get());
  const [user, setUser] = useState<AuthUser | null>(null);
  const [status, setStatus] = useState<AuthStatus>(() => (tokenStorage.get() ? "checking" : "unauthenticated"));

  const clearAuth = useCallback(() => {
    tokenStorage.clear();
    setToken(null);
    setUser(null);
    setStatus("unauthenticated");
  }, []);

  useEffect(() => {
    let isMounted = true;
    const storedToken = tokenStorage.get();

    if (!storedToken) {
      clearAuth();
      return;
    }

    setStatus("checking");

    getCurrentUser(storedToken)
      .then((currentUser) => {
        if (!isMounted) {
          return;
        }

        setToken(storedToken);
        setUser(currentUser);
        setStatus("authenticated");
      })
      .catch(() => {
        if (isMounted) {
          clearAuth();
        }
      });

    return () => {
      isMounted = false;
    };
  }, [clearAuth]);

  useEffect(() => {
    window.addEventListener(AUTH_UNAUTHORIZED_EVENT, clearAuth);

    return () => {
      window.removeEventListener(AUTH_UNAUTHORIZED_EVENT, clearAuth);
    };
  }, [clearAuth]);

  const login = useCallback(async (request: LoginRequest) => {
    const response = await loginAdmin(request);

    tokenStorage.set(response.accessToken);
    setToken(response.accessToken);
    setUser(response.user);
    setStatus("authenticated");
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      login,
      logout: clearAuth,
      status,
      token,
      user
    }),
    [clearAuth, login, status, token, user]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error("useAuth must be used within AuthProvider.");
  }

  return context;
}
