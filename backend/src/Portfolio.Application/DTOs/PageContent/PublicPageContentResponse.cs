namespace Portfolio.Application.DTOs.PageContent;

public sealed record PublicPageContentResponse(
    string Key,
    string HtmlContent,
    DateTime UpdatedAt);
