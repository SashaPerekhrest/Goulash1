using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

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

        if (!await dbContext.AdminUsers.AnyAsync(user => user.Login == login, cancellationToken))
        {
            dbContext.AdminUsers.Add(new AdminUser
            {
                Id = Guid.NewGuid(),
                Login = login,
                PasswordHash = PasswordHasher.Hash(password),
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
                HtmlContent = "<h1>О себе</h1><p>Стартовый контент портфолио.</p>",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

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
}
