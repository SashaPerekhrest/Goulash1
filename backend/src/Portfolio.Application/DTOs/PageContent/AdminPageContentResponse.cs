namespace Portfolio.Application.DTOs.PageContent;

public sealed record AdminPageContentResponse(
    Guid Id,
    string Key,
    string HtmlContent,
    DateTime CreatedAt,
    DateTime UpdatedAt);
