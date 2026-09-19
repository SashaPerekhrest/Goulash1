import Link from "next/link";
import type { Project } from "../lib/types";

type ProjectCardProps = {
  project: Project;
};

export function ProjectCard({ project }: ProjectCardProps) {
  return (
    <Link className="project-card" href={`/projects/${project.slug}`}>
      <div className="project-card__media" aria-hidden={!project.imageUrl}>
        {project.imageUrl ? (
          <img alt="" className="project-card__image" src={project.imageUrl} />
        ) : (
          <span className="project-card__fallback">
            {project.category === "AI" ? "AI" : "PR"}
          </span>
        )}
      </div>
      <div className="project-card__body">
        <h2>{project.title}</h2>
        <p>{project.shortDescription}</p>
      </div>
    </Link>
  );
}
