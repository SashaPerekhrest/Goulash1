using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.DTOs.Common;
using Portfolio.Application.DTOs.Projects;
using Portfolio.Api.Security;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(PortfolioDbContext dbContext) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ProjectListItemResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<ProjectListItemResponse>>> GetProjects(
        [FromQuery] string? category,
        CancellationToken cancellationToken)
    {
        if (!TryParseOptionalCategory(category, out var projectCategory))
        {
            return BadRequest(new ErrorResponse(
                "Validation failed.",
                new Dictionary<string, string[]>
                {
                    ["category"] = ["Category must be either AI or OTHER."]
                }));
        }

        var query = dbContext.Projects
            .AsNoTracking()
            .Where(project => project.IsPublished);

        if (projectCategory.HasValue)
        {
            query = query.Where(project => project.Category == projectCategory.Value);
        }

        var projects = await query
            .OrderByDescending(project => project.CreatedAt)
            .Select(project => new ProjectListItemResponse(
                project.Id,
                project.Title,
                project.Slug,
                project.ShortDescription,
                project.ImageUrl,
                project.Category,
                project.IsPublished,
                project.CreatedAt,
                project.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Ok(projects);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ProjectDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDetailResponse>> GetProject(
        string slug,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Slug == slug && project.IsPublished)
            .Select(project => ToDetailResponse(project))
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return NotFound(new ErrorResponse("Project not found."));
        }

        return Ok(project);
    }

    private static ProjectDetailResponse ToDetailResponse(Project project)
    {
        return new ProjectDetailResponse(
            project.Id,
            project.Title,
            project.Slug,
            project.ShortDescription,
            project.ImageUrl,
            HtmlSanitizer.Sanitize(project.HtmlContent),
            project.Category,
            project.IsPublished,
            project.CreatedAt,
            project.UpdatedAt);
    }

    private static bool TryParseOptionalCategory(string? category, out ProjectCategory? projectCategory)
    {
        projectCategory = null;

        if (string.IsNullOrWhiteSpace(category))
        {
            return true;
        }

        if (category == nameof(ProjectCategory.AI))
        {
            projectCategory = ProjectCategory.AI;
            return true;
        }

        if (category == nameof(ProjectCategory.OTHER))
        {
            projectCategory = ProjectCategory.OTHER;
            return true;
        }

        return false;
    }
}
