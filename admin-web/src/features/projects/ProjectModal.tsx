import { useEffect, useMemo, useRef, useState } from "react";
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Stack,
  Switch,
  TextField,
  Typography
} from "@mui/material";
import { useMutation } from "@tanstack/react-query";
import { Controller, useForm } from "react-hook-form";
import { TinyEditor } from "../../components/TinyEditor/TinyEditor";
import { ApiError } from "../../shared/api/client";
import { uploadAdminFile } from "../../shared/api/files";
import { createAdminProject, updateAdminProject } from "../../shared/api/projects";
import type { Project, ProjectCategory, ProjectFormRequest } from "../../shared/types/projects";

interface ProjectModalProps {
  category: ProjectCategory;
  onClose: () => void;
  onSaved: () => Promise<void> | void;
  open: boolean;
  project: Project | null;
}

interface ProjectFormValues {
  title: string;
  slug: string;
  imageUrl: string;
  shortDescription: string;
  htmlContent: string;
  isPublished: boolean;
}

const slugPattern = /^[A-Za-z0-9-]+$/;
const acceptedImageTypes = ["image/jpeg", "image/png", "image/webp", "image/svg+xml"];

export function ProjectModal({ category, onClose, onSaved, open, project }: ProjectModalProps) {
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  const defaultValues = useMemo(() => getDefaultValues(project), [project]);
  const {
    control,
    formState: { errors, isDirty },
    handleSubmit,
    reset,
    setError,
    setValue,
    watch
  } = useForm<ProjectFormValues>({
    defaultValues
  });

  useEffect(() => {
    if (open) {
      reset(defaultValues);
      setSubmitError(null);
      setUploadError(null);
      setFieldErrors({});
    }
  }, [defaultValues, open, reset]);

  const imageUrl = watch("imageUrl");

  const saveMutation = useMutation({
    mutationFn: (values: ProjectFormValues) => {
      const request = toRequest(values, category);

      return project
        ? updateAdminProject(project.id, request)
        : createAdminProject(request);
    },
    onSuccess: async () => {
      await onSaved();
    },
    onError: (error) => {
      const nextFieldErrors = getFieldErrors(error);
      setFieldErrors(nextFieldErrors);

      Object.entries(nextFieldErrors).forEach(([fieldName, message]) => {
        if (isProjectFormField(fieldName)) {
          setError(fieldName, { type: "server", message });
        }
      });

      if (error instanceof ApiError && error.status === 409) {
        setError("slug", { type: "server", message: "Проект с таким slug уже существует." });
        setSubmitError("Проект с таким slug уже существует.");
        return;
      }

      setSubmitError(getErrorMessage(error, "Не удалось сохранить проект."));
    }
  });

  const uploadMutation = useMutation({
    mutationFn: (file: File) => uploadAdminFile(file, "projects"),
    onSuccess: (response) => {
      setValue("imageUrl", response.url, { shouldDirty: true, shouldValidate: true });
      setUploadError(null);
    },
    onError: (error) => {
      setUploadError(getErrorMessage(error, "Не удалось загрузить изображение."));
    }
  });

  const isSaving = saveMutation.isPending;
  const isUploading = uploadMutation.isPending;
  const isBusy = isSaving || isUploading;

  const handleFileChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    event.target.value = "";

    if (!file) {
      return;
    }

    if (!acceptedImageTypes.includes(file.type)) {
      setUploadError("Загрузите jpeg, png, webp или svg.");
      return;
    }

    uploadMutation.mutate(file);
  };

  return (
    <Dialog
      fullWidth
      maxWidth={false}
      onClose={() => {
        if (!isBusy) {
          onClose();
        }
      }}
      open={open}
      slotProps={{
        paper: {
          sx: {
            height: { xs: "94vh", md: "90vh" },
            maxWidth: "min(1120px, 90vw)"
          }
        }
      }}
    >
      <DialogTitle>{project ? "Редактировать проект" : "Создать проект"}</DialogTitle>
      <DialogContent dividers>
        <Stack
          component="form"
          id="project-form"
          noValidate
          spacing={3}
          onSubmit={handleSubmit((values) => saveMutation.mutate(values))}
        >
          {submitError ? <Alert severity="error">{submitError}</Alert> : null}
          {fieldErrors.category ? <Alert severity="error">{fieldErrors.category}</Alert> : null}

          <Stack direction={{ xs: "column", md: "row" }} spacing={2.5}>
            <Controller
              control={control}
              name="title"
              rules={{
                required: "Введите название.",
                maxLength: { value: 200, message: "Название должно быть не длиннее 200 символов." },
                validate: (value) => value.trim().length > 0 || "Введите название."
              }}
              render={({ field, fieldState }) => (
                <TextField
                  {...field}
                  disabled={isBusy}
                  error={Boolean(fieldState.error)}
                  fullWidth
                  helperText={fieldState.error?.message}
                  label="Название"
                />
              )}
            />

            <Controller
              control={control}
              name="slug"
              rules={{
                required: "Введите slug.",
                maxLength: { value: 200, message: "Slug должен быть не длиннее 200 символов." },
                pattern: {
                  value: slugPattern,
                  message: "Используйте латиницу, цифры и дефисы."
                },
                validate: (value) => value.trim().length > 0 || "Введите slug."
              }}
              render={({ field, fieldState }) => (
                <TextField
                  {...field}
                  disabled={isBusy}
                  error={Boolean(fieldState.error)}
                  fullWidth
                  helperText={fieldState.error?.message ?? "/projects/example-slug"}
                  label="Slug"
                />
              )}
            />
          </Stack>

          <Controller
            control={control}
            name="shortDescription"
            rules={{
              required: "Введите краткое описание.",
              maxLength: { value: 500, message: "Описание должно быть не длиннее 500 символов." },
              validate: (value) => value.trim().length > 0 || "Введите краткое описание."
            }}
            render={({ field, fieldState }) => (
              <TextField
                {...field}
                disabled={isBusy}
                error={Boolean(fieldState.error)}
                fullWidth
                helperText={fieldState.error?.message}
                label="Краткое описание"
                minRows={3}
                multiline
              />
            )}
          />

          <Stack spacing={1.5}>
            <Controller
              control={control}
              name="imageUrl"
              rules={{
                maxLength: { value: 1000, message: "URL изображения должен быть не длиннее 1000 символов." }
              }}
              render={({ field, fieldState }) => (
                <TextField
                  {...field}
                  disabled={isBusy}
                  error={Boolean(fieldState.error)}
                  fullWidth
                  helperText={fieldState.error?.message}
                  label="URL изображения"
                />
              )}
            />

            <Stack direction={{ xs: "column", sm: "row" }} spacing={1.5} sx={{ alignItems: { xs: "stretch", sm: "center" } }}>
              <Button disabled={isBusy} onClick={() => fileInputRef.current?.click()} variant="outlined">
                {isUploading ? <CircularProgress color="inherit" size={20} /> : "Загрузить изображение"}
              </Button>
              <Button
                disabled={isBusy || !imageUrl}
                onClick={() => setValue("imageUrl", "", { shouldDirty: true, shouldValidate: true })}
              >
                Очистить
              </Button>
              <input
                ref={fileInputRef}
                accept={acceptedImageTypes.join(",")}
                hidden
                onChange={handleFileChange}
                type="file"
              />
            </Stack>

            {uploadError ? <Alert severity="error">{uploadError}</Alert> : null}
            {imageUrl ? <ImagePreview imageUrl={imageUrl} /> : null}
          </Stack>

          <Stack spacing={1}>
            <Typography component="label" htmlFor="project-html-content" sx={{ fontWeight: 600 }} variant="body2">
              Полное HTML-описание
            </Typography>
            <Controller
              control={control}
              name="htmlContent"
              rules={{
                required: "Введите полное описание.",
                validate: (value) => value.trim().length > 0 || "Введите полное описание."
              }}
              render={({ field }) => (
                <Box
                  sx={{
                    "& .tox-tinymce": {
                      borderColor: errors.htmlContent ? "error.main" : "divider",
                      borderRadius: 1
                    }
                  }}
                >
                  <TinyEditor
                    disabled={isBusy}
                    id="project-html-content"
                    onChange={field.onChange}
                    value={field.value}
                  />
                </Box>
              )}
            />
            {errors.htmlContent ? (
              <Typography color="error" variant="caption">
                {errors.htmlContent.message}
              </Typography>
            ) : null}
          </Stack>

          <Controller
            control={control}
            name="isPublished"
            render={({ field }) => (
              <FormControlLabel
                control={
                  <Switch
                    checked={field.value}
                    disabled={isBusy}
                    onChange={(_, checked) => field.onChange(checked)}
                  />
                }
                label={field.value ? "Опубликован" : "Черновик"}
              />
            )}
          />

          {isDirty ? null : (
            <Typography color="text.secondary" variant="caption">
              Изменений в форме пока нет.
            </Typography>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button disabled={isBusy} onClick={onClose}>
          Отмена
        </Button>
        <Button disabled={isBusy} form="project-form" type="submit" variant="contained">
          {isSaving ? "Сохранение..." : "Сохранить"}
        </Button>
      </DialogActions>
    </Dialog>
  );
}

function ImagePreview({ imageUrl }: { imageUrl: string }) {
  return (
    <Box
      sx={{
        alignItems: "center",
        border: 1,
        borderColor: "divider",
        borderRadius: 2,
        display: "flex",
        gap: 2,
        p: 1.5
      }}
    >
      <Box
        alt=""
        component="img"
        src={imageUrl}
        sx={{
          bgcolor: "action.hover",
          borderRadius: 1,
          height: 72,
          objectFit: "cover",
          width: 96
        }}
      />
      <Typography color="text.secondary" sx={{ overflowWrap: "anywhere" }} variant="body2">
        {imageUrl}
      </Typography>
    </Box>
  );
}

function getDefaultValues(project: Project | null): ProjectFormValues {
  return {
    title: project?.title ?? "",
    slug: project?.slug ?? "",
    imageUrl: project?.imageUrl ?? "",
    shortDescription: project?.shortDescription ?? "",
    htmlContent: project?.htmlContent ?? "",
    isPublished: project?.isPublished ?? false
  };
}

function toRequest(values: ProjectFormValues, category: ProjectCategory): ProjectFormRequest {
  return {
    title: values.title.trim(),
    slug: values.slug.trim(),
    shortDescription: values.shortDescription.trim(),
    imageUrl: values.imageUrl.trim() || null,
    htmlContent: values.htmlContent,
    category,
    isPublished: values.isPublished
  };
}

function getFieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || !error.errors) {
    return {};
  }

  return Object.fromEntries(
    Object.entries(error.errors).map(([fieldName, messages]) => [
      fieldName.charAt(0).toLowerCase() + fieldName.slice(1),
      messages[0] ?? "Некорректное значение."
    ])
  );
}

function isProjectFormField(fieldName: string): fieldName is keyof ProjectFormValues {
  return ["title", "slug", "imageUrl", "shortDescription", "htmlContent", "isPublished"].includes(fieldName);
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
