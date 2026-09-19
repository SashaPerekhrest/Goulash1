import type { Metadata } from "next";
import { getPageContent, isNotFoundError } from "../lib/api";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "О себе",
  description: "Опыт, навыки и подход к внедрению ИИ-решений."
};

export default function HomePage() {
  return <AboutPageContent />;
}

async function AboutPageContent() {
  try {
    const page = await getPageContent("about");
    const hasContent = page.htmlContent.trim().length > 0;

    return (
      <main className="page-shell page-shell--narrow">
        {hasContent ? (
          <article
            className="html-content"
            dangerouslySetInnerHTML={{ __html: page.htmlContent }}
          />
        ) : (
          <section className="empty-state">
            <h1>О себе</h1>
            <p>Контент страницы пока не заполнен.</p>
          </section>
        )}
      </main>
    );
  } catch (error) {
    if (isNotFoundError(error)) {
      return (
        <main className="page-shell page-shell--narrow">
          <section className="empty-state">
            <h1>О себе</h1>
            <p>Контент страницы пока не найден.</p>
          </section>
        </main>
      );
    }

    throw error;
  }
}
