import type { PageContent, Project, ProjectCategory } from "./types";

const API_BASE_URL = (
  process.env.API_URL ??
  process.env.NEXT_PUBLIC_API_URL ??
  "http://localhost:5000"
).replace(/\/$/, "");

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function request<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    cache: "no-store",
    headers: {
      Accept: "application/json"
    }
  });

  if (!response.ok) {
    throw new ApiError(`API request failed: ${path}`, response.status);
  }

  return (await response.json()) as T;
}

export function isNotFoundError(error: unknown) {
  return error instanceof ApiError && error.status === 404;
}

export async function getPageContent(key: string) {
  return request<PageContent>(`/api/pages/${encodeURIComponent(key)}`);
}

export async function getProjects(category?: ProjectCategory) {
  const query = category ? `?category=${encodeURIComponent(category)}` : "";

  return request<Project[]>(`/api/projects${query}`);
}

export async function getProjectBySlug(slug: string) {
  return request<Project>(`/api/projects/${encodeURIComponent(slug)}`);
}
