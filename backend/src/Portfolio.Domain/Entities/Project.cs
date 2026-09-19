using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Entities;

public sealed class Project
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string ShortDescription { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

    public string HtmlContent { get; set; } = string.Empty;

    public ProjectCategory Category { get; set; }

    public bool IsPublished { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
