import { tokenStorage } from "./tokenStorage";

export const AUTH_UNAUTHORIZED_EVENT = "portfolio-admin:unauthorized";

interface ErrorResponse {
  message?: string;
  errors?: Record<string, string[]>;
}

interface ApiRequestOptions extends RequestInit {
  skipUnauthorizedHandling?: boolean;
  token?: string | null;
}

export class ApiError extends Error {
  readonly status: number;
  readonly errors?: Record<string, string[]>;

  constructor(status: number, message: string, errors?: Record<string, string[]>) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errors = errors;
  }
}

const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5000").replace(/\/$/, "");

export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const { skipUnauthorizedHandling, token, ...requestOptions } = options;
  const headers = new Headers(requestOptions.headers);
  const authToken = token === undefined ? tokenStorage.get() : token;

  headers.set("Accept", "application/json");

  if (authToken) {
    headers.set("Authorization", `Bearer ${authToken}`);
  }

  if (requestOptions.body && !(requestOptions.body instanceof FormData) && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...requestOptions,
    headers
  });

  if (response.status === 401 && !skipUnauthorizedHandling) {
    tokenStorage.clear();
    window.dispatchEvent(new Event(AUTH_UNAUTHORIZED_EVENT));
  }

  if (!response.ok) {
    const errorBody = await readErrorResponse(response);
    throw new ApiError(
      response.status,
      errorBody.message || getDefaultErrorMessage(response.status),
      errorBody.errors
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

async function readErrorResponse(response: Response): Promise<ErrorResponse> {
  const contentType = response.headers.get("Content-Type");

  if (!contentType?.includes("application/json")) {
    return {};
  }

  try {
    return (await response.json()) as ErrorResponse;
  } catch {
    return {};
  }
}

function getDefaultErrorMessage(status: number): string {
  if (status === 401) {
    return "Требуется авторизация.";
  }

  if (status >= 500) {
    return "Сервер временно недоступен.";
  }

  return "Запрос не выполнен.";
}
