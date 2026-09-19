namespace Portfolio.Application.DTOs.Projects;

public sealed record CreateProjectRequest(
    string? Title,
    string? Slug,
    string? ShortDescription,
    string? ImageUrl,
    string? HtmlContent,
    string? Category,
    bool? IsPublished);
