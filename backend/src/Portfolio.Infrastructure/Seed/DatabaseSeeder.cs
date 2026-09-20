using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.Data;
using Portfolio.Infrastructure.Security;

namespace Portfolio.Infrastructure.Seed;

public static class DatabaseSeeder
{
    private const string AboutPageKey = "about";

    public static async Task SeedAsync(
        PortfolioDbContext dbContext,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var adminCreated = false;
        var login = configuration["Seed:Admin:Login"] ?? "admin";
        var password = configuration["Seed:Admin:Password"] ?? "admin";
        var passwordHasher = new Pbkdf2PasswordHasher();

        if (!await dbContext.AdminUsers.AnyAsync(user => user.Login == login, cancellationToken))
        {
            dbContext.AdminUsers.Add(new AdminUser
            {
                Id = Guid.NewGuid(),
                Login = login,
                PasswordHash = passwordHasher.Hash(password),
                CreatedAt = now,
                UpdatedAt = now
            });

            adminCreated = true;
        }

        if (!await dbContext.PageContents.AnyAsync(page => page.Key == AboutPageKey, cancellationToken))
        {
            dbContext.PageContents.Add(new PageContent
            {
                Id = Guid.NewGuid(),
                Key = AboutPageKey,
                HtmlContent = DemoAboutHtml,
                CreatedAt = now,
                UpdatedAt = now
            });
        }
        else
        {
            var aboutPage = await dbContext.PageContents
                .FirstAsync(page => page.Key == AboutPageKey, cancellationToken);

            if (aboutPage.HtmlContent.Contains("Стартовый контент портфолио.", StringComparison.OrdinalIgnoreCase))
            {
                aboutPage.HtmlContent = DemoAboutHtml;
                aboutPage.UpdatedAt = now;
            }
        }

        await SeedProjectAsync(
            dbContext,
            "AI Support Bot",
            "ai-support-bot",
            "Бот поддержки, который классифицирует обращения, предлагает ответы оператору и собирает базу повторяющихся вопросов.",
            ProjectCategory.AI,
            true,
            """
            <h1>AI Support Bot</h1>
            <p>Проект демонстрирует внедрение ИИ в поддержку пользователей: классификация обращений, генерация черновиков ответов и сбор аналитики по повторяющимся проблемам.</p>
            <h2>Что сделано</h2>
            <ul><li>Спроектирован backend API для интеграции с CRM.</li><li>Добавлен сценарий human-in-the-loop, где оператор подтверждает ответ.</li><li>Подготовлены метрики скорости реакции и качества подсказок.</li></ul>
            """,
            now,
            cancellationToken);

        await SeedProjectAsync(
            dbContext,
            "Knowledge Base RAG",
            "knowledge-base-rag",
            "Прототип поиска по внутренним документам с ответами на естественном языке и ссылками на источники.",
            ProjectCategory.AI,
            true,
            """
            <h1>Knowledge Base RAG</h1>
            <p>RAG-сценарий для команды, которая хочет быстро находить ответы в регламентах, инструкциях и проектной документации.</p>
            <h2>Фокус решения</h2>
            <ul><li>Разделение документов на фрагменты и подготовка индекса.</li><li>Ответы с цитированием источников.</li><li>Ограничение области поиска по типу документа.</li></ul>
            """,
            now.AddMinutes(-10),
            cancellationToken);

        await SeedProjectAsync(
            dbContext,
            "Integration Dashboard",
            "integration-dashboard",
            "Панель мониторинга интеграций: статусы обмена, последние ошибки и быстрый переход к журналам.",
            ProjectCategory.OTHER,
            true,
            """
            <h1>Integration Dashboard</h1>
            <p>Интерфейс для наблюдения за обменом данными между сервисами. Помогает быстро понять, какая интеграция требует внимания.</p>
            <h2>Возможности</h2>
            <ul><li>Сводка по статусам обмена.</li><li>Фильтрация событий по сервису и типу ошибки.</li><li>Отдельный API для детальной диагностики.</li></ul>
            """,
            now.AddMinutes(-20),
            cancellationToken);

        await SeedProjectAsync(
            dbContext,
            "Portfolio CMS",
            "portfolio-cms",
            "Fullstack-портфолио с публичным сайтом, защищенной админкой, PostgreSQL и S3-compatible файловым хранилищем.",
            ProjectCategory.OTHER,
            true,
            """
            <h1>Portfolio CMS</h1>
            <p>Этот проект: управляемый лендинг-портфолио, где публичная часть отделена от административной панели и работает через единый backend API.</p>
            <h2>Стек</h2>
            <ul><li>.NET 8 и PostgreSQL для backend.</li><li>Next.js для публичного сайта.</li><li>React, Vite, MUI и TinyMCE для админки.</li><li>SeaweedFS для хранения файлов.</li></ul>
            """,
            now.AddMinutes(-30),
            cancellationToken);

        await SeedProjectAsync(
            dbContext,
            "Draft Automation Scenario",
            "draft-automation-scenario",
            "Неопубликованный черновик для проверки, что публичный API не раскрывает проекты с isPublished=false.",
            ProjectCategory.AI,
            false,
            "<h1>Draft Automation Scenario</h1><p>Этот проект должен быть виден только в админке.</p>",
            now.AddMinutes(-40),
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (adminCreated)
        {
            LogDevelopmentAdminCreated(logger, login);
        }
    }

    private static void LogDevelopmentAdminCreated(ILogger logger, string login)
    {
        try
        {
            logger.LogWarning("Created development admin user '{Login}'. Override Seed:Admin settings outside local development.", login);
        }
        catch (Exception)
        {
            // Seed data must not fail because a local logging provider is unavailable.
        }
    }

    private static async Task SeedProjectAsync(
        PortfolioDbContext dbContext,
        string title,
        string slug,
        string shortDescription,
        ProjectCategory category,
        bool isPublished,
        string htmlContent,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        if (await dbContext.Projects.AnyAsync(project => project.Slug == slug, cancellationToken))
        {
            return;
        }

        dbContext.Projects.Add(new Project
        {
            Id = Guid.NewGuid(),
            Title = title,
            Slug = slug,
            ShortDescription = shortDescription,
            ImageUrl = null,
            HtmlContent = htmlContent,
            Category = category,
            IsPublished = isPublished,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        });
    }

    private const string DemoAboutHtml = """
        <h1>О себе</h1>
        <p><strong>Я специализируюсь на внедрении ИИ-решений в реальные рабочие процессы:</strong> от анализа задачи и проектирования сценария до backend-интеграций, интерфейсов и понятной демонстрации результата.</p>
        <h2>Чем полезен проект</h2>
        <p>Этот лендинг показывает не только резюме, но и инженерный подход: публичный сайт, защищенная админка, API, база данных, файловое хранилище и Docker-окружение собраны в один демонстрационный продукт.</p>
        <h2>Ключевые навыки</h2>
        <ul>
          <li>проектирование AI-сценариев и MVP для внедрения в процессы;</li>
          <li>backend на .NET, REST API, JWT, PostgreSQL и EF Core;</li>
          <li>frontend на React, Next.js, Vite и TypeScript;</li>
          <li>интеграции, автоматизация, работа с данными и файловыми хранилищами;</li>
          <li>подготовка решений к демонстрации: UX, безопасность, документация и запуск через Docker.</li>
        </ul>
        <h2>Подход к внедрению ИИ</h2>
        <p>Сначала фиксируется бизнес-сценарий и критерии пользы, затем собирается минимальная рабочая версия, проверяются риски качества и безопасности, после чего решение постепенно встраивается в существующий процесс.</p>
        """;
}
