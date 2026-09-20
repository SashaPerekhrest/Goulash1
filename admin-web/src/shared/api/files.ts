import { apiRequest } from "./client";
import type { FileUploadResponse } from "../types/projects";

export function uploadAdminFile(file: Blob, folder = "projects", fileName?: string): Promise<FileUploadResponse> {
  const formData = new FormData();
  formData.append("file", file, fileName);
  formData.append("folder", folder);

  return apiRequest<FileUploadResponse>("/api/admin/files", {
    method: "POST",
    body: formData
  });
}
