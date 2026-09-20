using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Portfolio.Infrastructure.Data;

public sealed class PortfolioDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();
        var apiSettingsPath = Path.GetFullPath(Path.Combine(basePath, "..", "Portfolio.Api"));

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.Exists(apiSettingsPath) ? apiSettingsPath : basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=portfolio;Username=portfolio;Password=portfolio_dev_password";

        var optionsBuilder = new DbContextOptionsBuilder<PortfolioDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new PortfolioDbContext(optionsBuilder.Options);
    }
}
