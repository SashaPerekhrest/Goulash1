import type { Metadata } from "next";
import { notFound } from "next/navigation";
import { getProjectBySlug, isNotFoundError } from "../../../lib/api";

export const dynamic = "force-dynamic";

type ProjectPageProps = {
  params: Promise<{
    slug: string;
  }>;
};

async function loadProject(slug: string) {
  try {
    return await getProjectBySlug(slug);
  } catch (error) {
    if (isNotFoundError(error)) {
      notFound();
    }

    throw error;
  }
}

export async function generateMetadata({
  params
}: ProjectPageProps): Promise<Metadata> {
  const { slug } = await params;
  const project = await loadProject(slug);

  return {
    title: project.title,
    description: project.shortDescription
  };
}

export default async function ProjectPage({ params }: ProjectPageProps) {
  const { slug } = await params;
  const project = await loadProject(slug);

  return (
    <main className="page-shell page-shell--narrow">
      <article className="project-detail">
        <div className="project-detail__meta">
          {project.category === "AI" ? "Проект с ИИ" : "Другой проект"}
        </div>
        <h1>{project.title}</h1>
        <p className="project-detail__lead">{project.shortDescription}</p>

        {project.imageUrl ? (
          <img
            alt=""
            className="project-detail__image"
            src={project.imageUrl}
          />
        ) : null}

        {project.htmlContent ? (
          <div
            className="html-content"
            dangerouslySetInnerHTML={{ __html: project.htmlContent }}
          />
        ) : (
          <div className="empty-state empty-state--compact">
            <h2>Описание пока не заполнено</h2>
            <p>Полное описание проекта появится после обновления контента.</p>
          </div>
        )}
      </article>
    </main>
  );
}
