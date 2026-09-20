import { useMemo, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography
} from "@mui/material";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ApiError } from "../../shared/api/client";
import { deleteAdminProject, getAdminProjects } from "../../shared/api/projects";
import type { Project, ProjectCategory } from "../../shared/types/projects";
import { ProjectModal } from "./ProjectModal";

interface ProjectsPageProps {
  category: ProjectCategory;
  title: string;
}

export function ProjectsPage({ category, title }: ProjectsPageProps) {
  const queryClient = useQueryClient();
  const [editingProject, setEditingProject] = useState<Project | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [projectToDelete, setProjectToDelete] = useState<Project | null>(null);

  const queryKey = useMemo(() => ["admin-projects", category] as const, [category]);
  const projectsQuery = useQuery({
    queryKey,
    queryFn: () => getAdminProjects(category)
  });

  const deleteMutation = useMutation({
    mutationFn: (projectId: string) => deleteAdminProject(projectId),
    onSuccess: async () => {
      setProjectToDelete(null);
      await queryClient.invalidateQueries({ queryKey });
    }
  });

  const openCreateModal = () => {
    setEditingProject(null);
    setIsModalOpen(true);
  };

  const openEditModal = (project: Project) => {
    setEditingProject(project);
    setIsModalOpen(true);
  };

  const closeModal = () => {
    setIsModalOpen(false);
    setEditingProject(null);
  };

  const loadErrorMessage = projectsQuery.error
    ? getErrorMessage(projectsQuery.error, "Не удалось загрузить проекты.")
    : null;
  const deleteErrorMessage = deleteMutation.error
    ? getErrorMessage(deleteMutation.error, "Не удалось удалить проект.")
    : null;

  return (
    <Stack spacing={3}>
      <Stack
        direction={{ xs: "column", sm: "row" }}
        spacing={2}
        sx={{ alignItems: { xs: "stretch", sm: "flex-start" }, justifyContent: "space-between" }}
      >
        <Box>
          <Typography component="h1" variant="h4">
            {title}
          </Typography>
          <Typography color="text.secondary" sx={{ mt: 1 }} variant="body1">
            Создание, редактирование, публикация и изображения проектов.
          </Typography>
        </Box>

        <Button onClick={openCreateModal} size="large" variant="contained">
          Создать
        </Button>
      </Stack>

      {loadErrorMessage ? (
        <Alert
          action={
            <Button color="inherit" onClick={() => projectsQuery.refetch()} size="small">
              Повторить
            </Button>
          }
          severity="error"
        >
          {loadErrorMessage}
        </Alert>
      ) : null}

      {deleteErrorMessage ? <Alert severity="error">{deleteErrorMessage}</Alert> : null}

      <ProjectsTable
        isLoading={projectsQuery.isLoading}
        onDelete={setProjectToDelete}
        onEdit={openEditModal}
        projects={projectsQuery.data ?? []}
      />

      <ProjectModal
        category={category}
        onClose={closeModal}
        onSaved={async () => {
          closeModal();
          await queryClient.invalidateQueries({ queryKey });
        }}
        open={isModalOpen}
        project={editingProject}
      />

      <Dialog
        fullWidth
        maxWidth="xs"
        onClose={() => {
          if (!deleteMutation.isPending) {
            setProjectToDelete(null);
          }
        }}
        open={Boolean(projectToDelete)}
      >
        <DialogTitle>Удалить проект?</DialogTitle>
        <DialogContent>
          <Typography color="text.secondary" variant="body2">
            Проект &quot;{projectToDelete?.title}&quot; будет удален из админки и публичного сайта.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button disabled={deleteMutation.isPending} onClick={() => setProjectToDelete(null)}>
            Отмена
          </Button>
          <Button
            color="error"
            disabled={deleteMutation.isPending || !projectToDelete}
            onClick={() => {
              if (projectToDelete) {
                deleteMutation.mutate(projectToDelete.id);
              }
            }}
            variant="contained"
          >
            {deleteMutation.isPending ? "Удаление..." : "Удалить"}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}

interface ProjectsTableProps {
  isLoading: boolean;
  onDelete: (project: Project) => void;
  onEdit: (project: Project) => void;
  projects: Project[];
}

function ProjectsTable({ isLoading, onDelete, onEdit, projects }: ProjectsTableProps) {
  if (isLoading) {
    return (
      <Box
        sx={{
          alignItems: "center",
          display: "flex",
          justifyContent: "center",
          minHeight: 320
        }}
      >
        <CircularProgress aria-label="Загрузка проектов" />
      </Box>
    );
  }

  if (projects.length === 0) {
    return (
      <Box
        sx={{
          border: 1,
          borderColor: "divider",
          borderRadius: 2,
          p: { xs: 3, sm: 4 },
          textAlign: "center"
        }}
      >
        <Typography component="h2" variant="h6">
          Проектов пока нет
        </Typography>
        <Typography color="text.secondary" sx={{ mt: 1 }} variant="body2">
          Создайте первый проект для этого раздела.
        </Typography>
      </Box>
    );
  }

  return (
    <TableContainer
      sx={{
        bgcolor: "background.paper",
        border: 1,
        borderColor: "divider",
        borderRadius: 2
      }}
    >
      <Table aria-label="Список проектов">
        <TableHead>
          <TableRow>
            <TableCell>Название</TableCell>
            <TableCell>Краткое описание</TableCell>
            <TableCell>Статус</TableCell>
            <TableCell align="right">Действия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {projects.map((project) => (
            <TableRow key={project.id} hover>
              <TableCell sx={{ minWidth: 220, verticalAlign: "top" }}>
                <Typography sx={{ fontWeight: 700 }} variant="body2">
                  {project.title}
                </Typography>
                <Typography color="text.secondary" variant="caption">
                  /projects/{project.slug}
                </Typography>
              </TableCell>
              <TableCell sx={{ minWidth: 280, verticalAlign: "top" }}>
                <Typography variant="body2">{project.shortDescription}</Typography>
              </TableCell>
              <TableCell sx={{ minWidth: 140, verticalAlign: "top" }}>
                <Chip
                  color={project.isPublished ? "success" : "default"}
                  label={project.isPublished ? "Опубликован" : "Черновик"}
                  size="small"
                  variant={project.isPublished ? "filled" : "outlined"}
                />
              </TableCell>
              <TableCell align="right" sx={{ minWidth: 180, verticalAlign: "top" }}>
                <Stack direction="row" spacing={1} sx={{ justifyContent: "flex-end" }}>
                  <Button onClick={() => onEdit(project)} size="small" variant="outlined">
                    Редактировать
                  </Button>
                  <Button color="error" onClick={() => onDelete(project)} size="small">
                    Удалить
                  </Button>
                </Stack>
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
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
