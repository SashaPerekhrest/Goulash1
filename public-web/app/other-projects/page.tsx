import type { Metadata } from "next";
import { EmptyState } from "../../components/EmptyState";
import { ProjectCard } from "../../components/ProjectCard";
import { getProjects } from "../../lib/api";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Другие проекты",
  description: "Инженерные проекты, подтверждающие опыт backend, frontend, интеграций и автоматизации."
};

export default async function OtherProjectsPage() {
  const projects = await getProjects("OTHER");

  return (
    <main className="page-shell">
      <section className="page-heading">
        <p className="eyebrow">Engineering projects</p>
        <h1>Другие проекты</h1>
        <p>
          Backend, frontend, интеграции и автоматизация: проекты, которые
          показывают инженерный опыт за пределами AI-направления.
        </p>
      </section>

      {projects.length > 0 ? (
        <section className="project-list" aria-label="Список других проектов">
          {projects.map((project) => (
            <ProjectCard key={project.id} project={project} />
          ))}
        </section>
      ) : (
        <EmptyState
          title="Проекты пока не опубликованы"
          text="Когда администратор опубликует другие проекты, они появятся в этом разделе."
        />
      )}
    </main>
  );
}
