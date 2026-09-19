using Portfolio.Domain.Enums;

namespace Portfolio.Application.DTOs.Projects;

public sealed record ProjectListItemResponse(
    Guid Id,
    string Title,
    string Slug,
    string ShortDescription,
    string? ImageUrl,
    ProjectCategory Category,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime UpdatedAt);
