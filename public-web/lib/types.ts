export type ProjectCategory = "AI" | "OTHER";

export type PageContent = {
  key: string;
  htmlContent: string;
  updatedAt: string;
};

export type Project = {
  id: string;
  title: string;
  slug: string;
  shortDescription: string;
  imageUrl?: string | null;
  htmlContent?: string;
  category: ProjectCategory;
  isPublished: boolean;
  createdAt: string;
  updatedAt: string;
};
