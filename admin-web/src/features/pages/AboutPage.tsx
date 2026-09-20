import { useEffect, useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Stack,
  Typography
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { TinyEditor } from "../../components/TinyEditor/TinyEditor";
import { ApiError } from "../../shared/api/client";
import { getAdminPageContent, updateAdminPageContent } from "../../shared/api/pageContent";
import type { AdminPageContent } from "../../shared/types/pageContent";

const ABOUT_PAGE_KEY = "about";
const aboutQueryKey = ["admin-page-content", ABOUT_PAGE_KEY] as const;

export function AboutPage() {
  const queryClient = useQueryClient();
  const [htmlContent, setHtmlContent] = useState("");
  const [lastSavedContent, setLastSavedContent] = useState("");
  const [showSuccess, setShowSuccess] = useState(false);

  const pageQuery = useQuery({
    queryKey: aboutQueryKey,
    queryFn: () => getAdminPageContent(ABOUT_PAGE_KEY)
  });

  useEffect(() => {
    if (!pageQuery.data) {
      return;
    }

    setHtmlContent(pageQuery.data.htmlContent);
    setLastSavedContent(pageQuery.data.htmlContent);
  }, [pageQuery.data]);

  const saveMutation = useMutation({
    mutationFn: (nextHtmlContent: string) =>
      updateAdminPageContent(ABOUT_PAGE_KEY, {
        htmlContent: nextHtmlContent
      }),
    onSuccess: (updatedPage) => {
      queryClient.setQueryData<AdminPageContent>(aboutQueryKey, updatedPage);
      setHtmlContent(updatedPage.htmlContent);
      setLastSavedContent(updatedPage.htmlContent);
      setShowSuccess(true);
    }
  });

  const isDirty = htmlContent !== lastSavedContent;
  const isSaving = saveMutation.isPending;
  const canSave = isDirty && htmlContent.trim().length > 0 && !isSaving;

  const loadErrorMessage = useMemo(() => {
    if (!pageQuery.error) {
      return null;
    }

    return getErrorMessage(pageQuery.error, "Не удалось загрузить контент страницы.");
  }, [pageQuery.error]);

  const saveErrorMessage = saveMutation.error
    ? getErrorMessage(saveMutation.error, "Не удалось сохранить контент страницы.")
    : null;

  const handleEditorChange = (value: string) => {
    setHtmlContent(value);
    setShowSuccess(false);
  };

  const handleSave = () => {
    saveMutation.mutate(htmlContent);
  };

  if (pageQuery.isLoading) {
    return (
      <Box
        sx={{
          alignItems: "center",
          display: "flex",
          justifyContent: "center",
          minHeight: 360
        }}
      >
        <CircularProgress aria-label="Загрузка контента страницы О себе" />
      </Box>
    );
  }

  if (pageQuery.isError) {
    return (
      <Stack spacing={3}>
        <PageHeader />
        <Alert severity="error">{loadErrorMessage}</Alert>
        <Box>
          <Button onClick={() => pageQuery.refetch()} variant="contained">
            Повторить загрузку
          </Button>
        </Box>
      </Stack>
    );
  }

  return (
    <Stack spacing={3}>
      <PageHeader />

      {showSuccess ? <Alert severity="success">Контент страницы сохранен.</Alert> : null}
      {saveErrorMessage ? <Alert severity="error">{saveErrorMessage}</Alert> : null}

      <Box
        sx={{
          "& .tox-tinymce": {
            borderColor: "divider",
            borderRadius: 1
          }
        }}
      >
        <TinyEditor disabled={isSaving} id="about-html-content" onChange={handleEditorChange} value={htmlContent} />
      </Box>

      <Stack
        direction={{ xs: "column", sm: "row" }}
        spacing={2}
        sx={{ alignItems: { xs: "stretch", sm: "center" }, justifyContent: "space-between" }}
      >
        <Typography color="text.secondary" variant="body2">
          {isDirty ? "Есть несохраненные изменения." : "Все изменения сохранены."}
        </Typography>

        <Button disabled={!canSave} onClick={handleSave} variant="contained">
          {isSaving ? "Сохранение..." : "Сохранить"}
        </Button>
      </Stack>
    </Stack>
  );
}

function PageHeader() {
  return (
    <Box>
      <Typography component="h1" variant="h4">
        О себе
      </Typography>
      <Typography color="text.secondary" sx={{ mt: 1 }} variant="body1">
        HTML-контент главной страницы редактируется через TinyMCE.
      </Typography>
    </Box>
  );
}

function getErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error && error.message) {
    return error.message;
  }

  return fallback;
}
