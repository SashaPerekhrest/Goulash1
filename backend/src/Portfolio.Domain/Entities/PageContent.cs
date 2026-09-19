namespace Portfolio.Domain.Entities;

public sealed class PageContent
{
    public Guid Id { get; set; }

    public string Key { get; set; } = string.Empty;

    public string HtmlContent { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
