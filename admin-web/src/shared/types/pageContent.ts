export interface AdminPageContent {
  id: string;
  key: string;
  htmlContent: string;
  createdAt: string;
  updatedAt: string;
}

export interface UpdatePageContentRequest {
  htmlContent: string;
}
