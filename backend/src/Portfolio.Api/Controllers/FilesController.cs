using System.Net.Http.Headers;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs.Common;
using Portfolio.Application.DTOs.Files;
using Portfolio.Application.Interfaces;

namespace Portfolio.Api.Controllers;

[ApiController]
[Route("api/admin/files")]
public sealed class FilesController(IFileStorageService fileStorageService) : ControllerBase
{
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/svg+xml"
    };

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FileUploadResponse>> Upload(
        [FromForm] IFormFile? file,
        [FromForm] string? folder,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateFile(file, out var contentType);
        if (file is not null && contentType == "image/svg+xml")
        {
            var svgErrors = await ValidateSvgFileAsync(file, cancellationToken);
            foreach (var error in svgErrors)
            {
                validationErrors[error.Key] = error.Value;
            }
        }

        if (validationErrors.Count > 0)
        {
            return BadRequest(new ErrorResponse("Validation failed.", validationErrors));
        }

        await using var stream = file!.OpenReadStream();
        var response = await fileStorageService.UploadAsync(
            stream,
            file.FileName,
            contentType!,
            file.Length,
            folder,
            cancellationToken);

        return Created(response.Url, response);
    }

    private static Dictionary<string, string[]> ValidateFile(IFormFile? file, out string? contentType)
    {
        contentType = null;
        var errors = new Dictionary<string, string[]>();

        if (file is null)
        {
            errors["file"] = ["File is required."];
            return errors;
        }

        if (file.Length <= 0)
        {
            errors["file"] = ["File is empty."];
        }
        else if (file.Length > MaxFileSizeBytes)
        {
            errors["file"] = ["File size must be 5 MB or smaller."];
        }

        contentType = NormalizeContentType(file.ContentType);
        if (contentType is null || !AllowedContentTypes.Contains(contentType))
        {
            errors["contentType"] = ["File type must be image/jpeg, image/png, image/webp, or image/svg+xml."];
        }

        return errors;
    }

    private static async Task<Dictionary<string, string[]>> ValidateSvgFileAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        try
        {
            await using var stream = file.OpenReadStream();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                Async = true,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });

            var rootElementSeen = false;

            while (await reader.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (reader.NodeType != XmlNodeType.Element)
                {
                    continue;
                }

                var elementName = reader.LocalName;
                if (!rootElementSeen)
                {
                    rootElementSeen = true;
                    if (!elementName.Equals("svg", StringComparison.OrdinalIgnoreCase))
                    {
                        errors["file"] = ["SVG file must have an svg root element."];
                        return errors;
                    }
                }

                if (IsDangerousSvgElement(elementName))
                {
                    errors["file"] = ["SVG file contains unsupported active content."];
                    return errors;
                }

                if (HasDangerousSvgAttribute(reader))
                {
                    errors["file"] = ["SVG file contains unsafe attributes or links."];
                    return errors;
                }
            }

            if (!rootElementSeen)
            {
                errors["file"] = ["SVG file is empty or invalid."];
            }
        }
        catch (XmlException)
        {
            errors["file"] = ["SVG file is not valid XML."];
        }

        return errors;
    }

    private static string? NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        return MediaTypeHeaderValue.TryParse(contentType, out var parsedContentType)
            ? parsedContentType.MediaType
            : contentType.Trim();
    }

    private static bool IsDangerousSvgElement(string elementName)
    {
        return elementName.Equals("script", StringComparison.OrdinalIgnoreCase)
            || elementName.Equals("foreignObject", StringComparison.OrdinalIgnoreCase)
            || elementName.Equals("iframe", StringComparison.OrdinalIgnoreCase)
            || elementName.Equals("object", StringComparison.OrdinalIgnoreCase)
            || elementName.Equals("embed", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasDangerousSvgAttribute(XmlReader reader)
    {
        if (!reader.HasAttributes)
        {
            return false;
        }

        while (reader.MoveToNextAttribute())
        {
            var name = reader.LocalName;
            var value = reader.Value.Trim();

            if (name.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                || name.Equals("style", StringComparison.OrdinalIgnoreCase)
                || IsDangerousSvgUrlAttribute(name, value))
            {
                reader.MoveToElement();
                return true;
            }
        }

        reader.MoveToElement();
        return false;
    }

    private static bool IsDangerousSvgUrlAttribute(string name, string value)
    {
        if (!name.Equals("href", StringComparison.OrdinalIgnoreCase)
            && !name.Equals("src", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("vbscript:", StringComparison.OrdinalIgnoreCase);
    }
}
