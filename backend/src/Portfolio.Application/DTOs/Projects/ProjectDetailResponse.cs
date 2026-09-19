using Portfolio.Domain.Enums;

namespace Portfolio.Application.DTOs.Projects;

public sealed record ProjectDetailResponse(
    Guid Id,
    string Title,
    string Slug,
    string ShortDescription,
    string? ImageUrl,
    string HtmlContent,
    ProjectCategory Category,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt);
