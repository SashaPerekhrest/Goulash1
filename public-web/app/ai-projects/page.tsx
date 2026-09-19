import type { Metadata } from "next";
import { EmptyState } from "../../components/EmptyState";
import { ProjectCard } from "../../components/ProjectCard";
import { getProjects } from "../../lib/api";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Проекты с ИИ",
  description: "Проекты с использованием искусственного интеллекта, автоматизации и интеграций."
};

export default async function AiProjectsPage() {
  const projects = await getProjects("AI");

  return (
    <main className="page-shell">
      <section className="page-heading">
        <p className="eyebrow">AI projects</p>
        <h1>Проекты с ИИ</h1>
        <p>
          Практические решения, где искусственный интеллект помогает ускорять
          процессы, работать с данными и улучшать пользовательский опыт.
        </p>
      </section>

      {projects.length > 0 ? (
        <section className="project-list" aria-label="Список проектов с ИИ">
          {projects.map((project) => (
            <ProjectCard key={project.id} project={project} />
          ))}
        </section>
      ) : (
        <EmptyState
          title="Проекты пока не опубликованы"
          text="Когда администратор опубликует AI-проекты, они появятся в этом разделе."
        />
      )}
    </main>
  );
}
