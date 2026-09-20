import { apiRequest } from "./client";
import type { FileUploadResponse } from "../types/projects";

export function uploadAdminFile(file: File, folder = "projects"): Promise<FileUploadResponse> {
  const formData = new FormData();
  formData.append("file", file);
  formData.append("folder", folder);

  return apiRequest<FileUploadResponse>("/api/admin/files", {
    method: "POST",
    body: formData
  });
}
