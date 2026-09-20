import { apiRequest } from "./client";
import type { Project, ProjectCategory, ProjectFormRequest } from "../types/projects";

export function getAdminProjects(category: ProjectCategory): Promise<Project[]> {
  return apiRequest<Project[]>(`/api/admin/projects?category=${encodeURIComponent(category)}`, {
    method: "GET"
  });
}

export function createAdminProject(request: ProjectFormRequest): Promise<Project> {
  return apiRequest<Project>("/api/admin/projects", {
    method: "POST",
    body: JSON.stringify(request)
  });
}

export function updateAdminProject(id: string, request: ProjectFormRequest): Promise<Project> {
  return apiRequest<Project>(`/api/admin/projects/${id}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}

export function deleteAdminProject(id: string): Promise<void> {
  return apiRequest<void>(`/api/admin/projects/${id}`, {
    method: "DELETE"
  });
}
