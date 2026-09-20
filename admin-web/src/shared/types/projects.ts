export type ProjectCategory = "AI" | "OTHER";

export interface Project {
  id: string;
  title: string;
  slug: string;
  shortDescription: string;
  imageUrl: string | null;
  htmlContent: string;
  category: ProjectCategory;
  isPublished: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface ProjectFormRequest {
  title: string;
  slug: string;
  shortDescription: string;
  imageUrl: string | null;
  htmlContent: string;
  category: ProjectCategory;
  isPublished: boolean;
}

export interface FileUploadResponse {
  fileName: string;
  contentType: string;
  size: number;
  url: string;
}
