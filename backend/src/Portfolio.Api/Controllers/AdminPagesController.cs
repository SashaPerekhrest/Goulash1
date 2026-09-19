using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.DTOs.Common;
using Portfolio.Application.DTOs.PageContent;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/admin/pages")]
public sealed class AdminPagesController(PortfolioDbContext dbContext) : ControllerBase
{
    private const int MaxHtmlContentLength = 100_000;

    [HttpGet("{key}")]
    [ProducesResponseType(typeof(AdminPageContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminPageContentResponse>> GetPageContent(
        string key,
        CancellationToken cancellationToken)
    {
        var pageContent = await dbContext.PageContents
            .AsNoTracking()
            .Where(page => page.Key == key)
            .Select(page => ToAdminResponse(page))
            .FirstOrDefaultAsync(cancellationToken);

        if (pageContent is null)
        {
            return NotFound(new ErrorResponse("Page content not found."));
        }

        return Ok(pageContent);
    }

    [HttpPut("{key}")]
    [ProducesResponseType(typeof(AdminPageContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminPageContentResponse>> UpdatePageContent(
        string key,
        UpdatePageContentRequest request,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateRequest(request);
        if (validationErrors.Count > 0)
        {
            return BadRequest(new ErrorResponse("Validation failed.", validationErrors));
        }

        var pageContent = await dbContext.PageContents
            .FirstOrDefaultAsync(page => page.Key == key, cancellationToken);

        if (pageContent is null)
        {
            return NotFound(new ErrorResponse("Page content not found."));
        }

        pageContent.HtmlContent = request.HtmlContent!;
        pageContent.UpdatedAt = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ToAdminResponse(pageContent));
    }

    private static Dictionary<string, string[]> ValidateRequest(UpdatePageContentRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.HtmlContent))
        {
            errors["htmlContent"] = ["HTML content is required."];
        }
        else if (request.HtmlContent.Length > MaxHtmlContentLength)
        {
            errors["htmlContent"] = [$"HTML content must be {MaxHtmlContentLength} characters or fewer."];
        }

        return errors;
    }

    private static AdminPageContentResponse ToAdminResponse(PageContent pageContent)
    {
        return new AdminPageContentResponse(
            pageContent.Id,
            pageContent.Key,
            pageContent.HtmlContent,
            pageContent.CreatedAt,
            pageContent.UpdatedAt);
    }
}
