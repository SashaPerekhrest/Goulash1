using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Portfolio.Application.DTOs.Common;
using Portfolio.Application.DTOs.PageContent;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/pages")]
public sealed class PagesController(PortfolioDbContext dbContext) : ControllerBase
{
    [HttpGet("{key}")]
    [ProducesResponseType(typeof(PublicPageContentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicPageContentResponse>> GetPageContent(
        string key,
        CancellationToken cancellationToken)
    {
        var pageContent = await dbContext.PageContents
            .AsNoTracking()
            .Where(page => page.Key == key)
            .Select(page => new PublicPageContentResponse(
                page.Key,
                page.HtmlContent,
                page.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        if (pageContent is null)
        {
            return NotFound(new ErrorResponse("Page content not found."));
        }

        return Ok(pageContent);
    }
}
