import { apiRequest } from "./client";
import type { AuthUser, LoginRequest, LoginResponse } from "../types/auth";

export function loginAdmin(request: LoginRequest): Promise<LoginResponse> {
  return apiRequest<LoginResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify(request),
    skipUnauthorizedHandling: true,
    token: null
  });
}

export function getCurrentUser(token?: string | null): Promise<AuthUser> {
  return apiRequest<AuthUser>("/api/auth/me", {
    method: "GET",
    token
  });
}
