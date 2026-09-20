using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Infrastructure.Data;

public sealed class PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();

    public DbSet<PageContent> PageContents => Set<PageContent>();

    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("admin_users");

            entity.HasKey(user => user.Id);

            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.Login).HasColumnName("login").HasMaxLength(100).IsRequired();
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(user => user.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(user => user.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasIndex(user => user.Login).IsUnique();
        });

        modelBuilder.Entity<PageContent>(entity =>
        {
            entity.ToTable("page_contents");

            entity.HasKey(page => page.Id);

            entity.Property(page => page.Id).HasColumnName("id");
            entity.Property(page => page.Key).HasColumnName("key").HasMaxLength(100).IsRequired();
            entity.Property(page => page.HtmlContent).HasColumnName("html_content").IsRequired();
            entity.Property(page => page.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(page => page.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasIndex(page => page.Key).IsUnique();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.ToTable("projects");

            entity.HasKey(project => project.Id);

            entity.Property(project => project.Id).HasColumnName("id");
            entity.Property(project => project.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            entity.Property(project => project.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();
            entity.Property(project => project.ShortDescription).HasColumnName("short_description").HasMaxLength(500).IsRequired();
            entity.Property(project => project.ImageUrl).HasColumnName("image_url").HasMaxLength(1000);
            entity.Property(project => project.HtmlContent).HasColumnName("html_content").IsRequired();
            entity.Property(project => project.Category)
                .HasColumnName("category")
                .HasMaxLength(20)
                .HasConversion(
                    category => category.ToString(),
                    value => Enum.Parse<ProjectCategory>(value))
                .IsRequired();
            entity.Property(project => project.IsPublished).HasColumnName("is_published").HasDefaultValue(false).IsRequired();
            entity.Property(project => project.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(project => project.UpdatedAt).HasColumnName("updated_at").IsRequired();

            entity.HasIndex(project => project.Slug).IsUnique();
        });
    }
}
