using System.Text.RegularExpressions;
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
[Route("api/admin/projects")]
public sealed partial class AdminProjectsController(PortfolioDbContext dbContext) : ControllerBase
{
    private const int MaxTitleLength = 200;
    private const int MaxSlugLength = 200;
    private const int MaxShortDescriptionLength = 500;
    private const int MaxImageUrlLength = 1000;

    [HttpGet]
    [ProducesResponseType(typeof(ProjectDetailResponse[]), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ProjectDetailResponse>>> GetProjects(
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

        var query = dbContext.Projects.AsNoTracking();

        if (projectCategory.HasValue)
        {
            query = query.Where(project => project.Category == projectCategory.Value);
        }

        var projects = await query
            .OrderByDescending(project => project.CreatedAt)
            .Select(project => ToDetailResponse(project))
            .ToListAsync(cancellationToken);

        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectDetailResponse>> GetProject(
        Guid id,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .AsNoTracking()
            .Where(project => project.Id == id)
            .Select(project => ToDetailResponse(project))
            .FirstOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return NotFound(new ErrorResponse("Project not found."));
        }

        return Ok(project);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProjectDetailResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectDetailResponse>> CreateProject(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ErrorResponse("Validation failed.", validationErrors));
        }

        var slug = request.Slug!.Trim();
        var slugExists = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(project => project.Slug == slug, cancellationToken);

        if (slugExists)
        {
            return Conflict(new ErrorResponse("Project with this slug already exists."));
        }

        var now = DateTime.UtcNow;
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Title = request.Title!.Trim(),
            Slug = slug,
            ShortDescription = request.ShortDescription!.Trim(),
            ImageUrl = NormalizeOptionalString(request.ImageUrl),
            HtmlContent = HtmlSanitizer.Sanitize(request.HtmlContent!),
            Category = ParseCategory(request.Category!),
            IsPublished = request.IsPublished!.Value,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.Projects.Add(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(
            nameof(GetProject),
            new { id = project.Id },
            ToDetailResponse(project));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProjectDetailResponse>> UpdateProject(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ErrorResponse("Validation failed.", validationErrors));
        }

        var project = await dbContext.Projects
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);

        if (project is null)
        {
            return NotFound(new ErrorResponse("Project not found."));
        }

        var slug = request.Slug!.Trim();
        var slugExists = await dbContext.Projects
            .AsNoTracking()
            .AnyAsync(otherProject => otherProject.Id != id && otherProject.Slug == slug, cancellationToken);

        if (slugExists)
        {
            return Conflict(new ErrorResponse("Project with this slug already exists."));
        }

        project.Title = request.Title!.Trim();
        project.Slug = slug;
        project.ShortDescription = request.ShortDescription!.Trim();
        project.ImageUrl = NormalizeOptionalString(request.ImageUrl);
        project.HtmlContent = HtmlSanitizer.Sanitize(request.HtmlContent!);
        project.Category = ParseCategory(request.Category!);
        project.IsPublished = request.IsPublished!.Value;
        project.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToDetailResponse(project));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(Guid id, CancellationToken cancellationToken)
    {
        var project = await dbContext.Projects
            .FirstOrDefaultAsync(project => project.Id == id, cancellationToken);

        if (project is null)
        {
            return NotFound(new ErrorResponse("Project not found."));
        }

        dbContext.Projects.Remove(project);
        await dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static Dictionary<string, string[]> ValidateRequest(CreateProjectRequest request)
    {
        return ValidateProjectFields(
            request.Title,
            request.Slug,
            request.ShortDescription,
            request.ImageUrl,
            request.HtmlContent,
            request.Category,
            request.IsPublished);
    }

    private static Dictionary<string, string[]> ValidateRequest(UpdateProjectRequest request)
    {
        return ValidateProjectFields(
            request.Title,
            request.Slug,
            request.ShortDescription,
            request.ImageUrl,
            request.HtmlContent,
            request.Category,
            request.IsPublished);
    }

    private static Dictionary<string, string[]> ValidateProjectFields(
        string? title,
        string? slug,
        string? shortDescription,
        string? imageUrl,
        string? htmlContent,
        string? category,
        bool? isPublished)
    {
        var errors = new Dictionary<string, string[]>();

        ValidateRequiredText(errors, "title", title, MaxTitleLength, "Title");
        ValidateSlug(errors, slug);
        ValidateRequiredText(errors, "shortDescription", shortDescription, MaxShortDescriptionLength, "Short description");

        if (!string.IsNullOrWhiteSpace(imageUrl) && imageUrl.Trim().Length > MaxImageUrlLength)
        {
            errors["imageUrl"] = [$"Image URL must be {MaxImageUrlLength} characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(htmlContent))
        {
            errors["htmlContent"] = ["HTML content is required."];
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            errors["category"] = ["Category is required."];
        }
        else if (!IsValidCategory(category))
        {
            errors["category"] = ["Category must be either AI or OTHER."];
        }

        if (!isPublished.HasValue)
        {
            errors["isPublished"] = ["Publication status is required."];
        }

        return errors;
    }

    private static void ValidateRequiredText(
        IDictionary<string, string[]> errors,
        string fieldName,
        string? value,
        int maxLength,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[fieldName] = [$"{displayName} is required."];
            return;
        }

        if (value.Trim().Length > maxLength)
        {
            errors[fieldName] = [$"{displayName} must be {maxLength} characters or fewer."];
        }
    }

    private static void ValidateSlug(IDictionary<string, string[]> errors, string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            errors["slug"] = ["Slug is required."];
            return;
        }

        var trimmedSlug = slug.Trim();
        if (trimmedSlug.Length > MaxSlugLength)
        {
            errors["slug"] = [$"Slug must be {MaxSlugLength} characters or fewer."];
            return;
        }

        if (!SlugRegex().IsMatch(trimmedSlug))
        {
            errors["slug"] = ["Slug can contain only Latin letters, numbers, and hyphens."];
        }
    }

    private static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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

    private static bool IsValidCategory(string category)
    {
        return category == nameof(ProjectCategory.AI)
            || category == nameof(ProjectCategory.OTHER);
    }

    private static ProjectCategory ParseCategory(string category)
    {
        return category == nameof(ProjectCategory.AI)
            ? ProjectCategory.AI
            : ProjectCategory.OTHER;
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

    [GeneratedRegex("^[A-Za-z0-9-]+$")]
    private static partial Regex SlugRegex();
}
