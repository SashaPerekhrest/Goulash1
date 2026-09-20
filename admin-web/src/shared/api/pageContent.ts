import { apiRequest } from "./client";
import type { AdminPageContent, UpdatePageContentRequest } from "../types/pageContent";

export function getAdminPageContent(key: string): Promise<AdminPageContent> {
  return apiRequest<AdminPageContent>(`/api/admin/pages/${key}`, {
    method: "GET"
  });
}

export function updateAdminPageContent(
  key: string,
  request: UpdatePageContentRequest
): Promise<AdminPageContent> {
  return apiRequest<AdminPageContent>(`/api/admin/pages/${key}`, {
    method: "PUT",
    body: JSON.stringify(request)
  });
}
