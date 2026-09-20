using System.Net;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace Portfolio.Api.Security;

public static partial class HtmlSanitizer
{
    private static readonly ISet<string> AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "a",
        "b",
        "blockquote",
        "br",
        "caption",
        "code",
        "col",
        "colgroup",
        "div",
        "em",
        "h1",
        "h2",
        "h3",
        "h4",
        "h5",
        "h6",
        "hr",
        "i",
        "img",
        "li",
        "ol",
        "p",
        "pre",
        "s",
        "span",
        "strong",
        "table",
        "tbody",
        "td",
        "tfoot",
        "th",
        "thead",
        "tr",
        "u",
        "ul"
    };

    private static readonly ISet<string> VoidTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "br",
        "col",
        "hr",
        "img"
    };

    private static readonly IReadOnlyDictionary<string, ISet<string>> AllowedAttributes =
        new Dictionary<string, ISet<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["a"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "href",
                "rel",
                "target",
                "title"
            },
            ["img"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "alt",
                "height",
                "src",
                "style",
                "title",
                "width"
            },
            ["td"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "colspan",
                "height",
                "rowspan",
                "style",
                "valign",
                "width"
            },
            ["table"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "height",
                "style",
                "width"
            },
            ["th"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "colspan",
                "height",
                "rowspan",
                "style",
                "valign",
                "width"
            },
            ["col"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "span",
                "style",
                "width"
            }
        };

    public static string Sanitize(string html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return string.Empty;
        }

        var sanitized = CommentRegex().Replace(html, string.Empty);
        sanitized = DangerousContentRegex().Replace(sanitized, string.Empty);
        sanitized = DangerousTagRegex().Replace(sanitized, string.Empty);

        return TagRegex().Replace(sanitized, SanitizeTag);
    }

    private static string SanitizeTag(Match match)
    {
        var tagName = match.Groups["name"].Value.ToLowerInvariant();
        if (!AllowedTags.Contains(tagName))
        {
            return string.Empty;
        }

        var isClosingTag = match.Groups["closing"].Success;
        if (isClosingTag)
        {
            return VoidTags.Contains(tagName) ? string.Empty : $"</{tagName}>";
        }

        var attributes = SanitizeAttributes(tagName, match.Groups["attrs"].Value);
        var suffix = VoidTags.Contains(tagName) ? " />" : ">";

        return string.IsNullOrEmpty(attributes)
            ? $"<{tagName}{suffix}"
            : $"<{tagName} {attributes}{suffix}";
    }

    private static string SanitizeAttributes(string tagName, string rawAttributes)
    {
        if (!AllowedAttributes.TryGetValue(tagName, out var allowedAttributes))
        {
            return string.Empty;
        }

        var attributes = new List<string>();
        var hasTargetBlank = false;
        var hasRel = false;

        foreach (Match attributeMatch in AttributeRegex().Matches(rawAttributes))
        {
            var attributeName = attributeMatch.Groups["name"].Value.ToLowerInvariant();
            if (!allowedAttributes.Contains(attributeName)
                || attributeName.StartsWith("on", StringComparison.OrdinalIgnoreCase)
                || attributeName.Equals("srcdoc", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var attributeValue = GetAttributeValue(attributeMatch);
            if (attributeName.Equals("style", StringComparison.OrdinalIgnoreCase))
            {
                var safeStyle = SanitizeStyle(tagName, attributeValue);
                if (!string.IsNullOrWhiteSpace(safeStyle))
                {
                    attributes.Add($"style=\"{HtmlEncoder.Default.Encode(safeStyle)}\"");
                }

                continue;
            }

            if (RequiresSafeUrl(attributeName) && !IsSafeUrl(attributeValue))
            {
                continue;
            }

            if ((attributeName is "width" or "height")
                && !IsSafeDimensionAttributeValue(attributeValue))
            {
                continue;
            }

            if ((attributeName is "colspan" or "rowspan" or "span")
                && !IsPositiveInteger(attributeValue))
            {
                continue;
            }

            if (attributeName == "valign" && !IsSafeVerticalAlign(attributeValue))
            {
                continue;
            }

            if (attributeName == "target")
            {
                if (attributeValue != "_blank")
                {
                    continue;
                }

                hasTargetBlank = true;
            }

            if (attributeName == "rel")
            {
                hasRel = true;
            }

            attributes.Add($"{attributeName}=\"{HtmlEncoder.Default.Encode(attributeValue)}\"");
        }

        if (tagName == "a" && hasTargetBlank && !hasRel)
        {
            attributes.Add("rel=\"noopener noreferrer\"");
        }

        return string.Join(" ", attributes);
    }

    private static string SanitizeStyle(string tagName, string rawStyle)
    {
        if (!AllowsStyleAttribute(tagName)
            || string.IsNullOrWhiteSpace(rawStyle)
            || ContainsDangerousStyleContent(rawStyle))
        {
            return string.Empty;
        }

        var declarations = new List<string>();
        foreach (var rawDeclaration in rawStyle.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separatorIndex = rawDeclaration.IndexOf(':', StringComparison.Ordinal);
            if (separatorIndex <= 0)
            {
                continue;
            }

            var propertyName = rawDeclaration[..separatorIndex].Trim().ToLowerInvariant();
            var propertyValue = rawDeclaration[(separatorIndex + 1)..].Trim().ToLowerInvariant();

            if (IsSafeStyle(tagName, propertyName, propertyValue))
            {
                declarations.Add($"{propertyName}: {propertyValue}");
            }
        }

        return string.Join("; ", declarations);
    }

    private static bool AllowsStyleAttribute(string tagName)
    {
        return tagName.Equals("img", StringComparison.OrdinalIgnoreCase)
            || tagName.Equals("table", StringComparison.OrdinalIgnoreCase)
            || tagName.Equals("td", StringComparison.OrdinalIgnoreCase)
            || tagName.Equals("th", StringComparison.OrdinalIgnoreCase)
            || tagName.Equals("col", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsDangerousStyleContent(string rawStyle)
    {
        return rawStyle.Contains("url(", StringComparison.OrdinalIgnoreCase)
            || rawStyle.Contains("expression(", StringComparison.OrdinalIgnoreCase)
            || rawStyle.Contains("javascript:", StringComparison.OrdinalIgnoreCase)
            || rawStyle.Contains("vbscript:", StringComparison.OrdinalIgnoreCase)
            || rawStyle.Contains("-moz-binding", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSafeStyle(string tagName, string propertyName, string propertyValue)
    {
        if (tagName.Equals("img", StringComparison.OrdinalIgnoreCase))
        {
            return IsSafeImageStyle(propertyName, propertyValue);
        }

        return IsSafeTableStyle(propertyName, propertyValue);
    }

    private static bool IsSafeImageStyle(string propertyName, string propertyValue)
    {
        if (propertyName == "float")
        {
            return propertyValue is "left" or "right" or "none";
        }

        if (propertyName == "display")
        {
            return propertyValue is "block" or "inline" or "inline-block";
        }

        if (propertyName is "margin" or "margin-left" or "margin-right" or "margin-top" or "margin-bottom")
        {
            return IsSafeSpacingValue(propertyValue);
        }

        if (propertyName is "width" or "height" or "max-width")
        {
            return IsSafeLengthValue(propertyValue);
        }

        return false;
    }

    private static bool IsSafeTableStyle(string propertyName, string propertyValue)
    {
        if (propertyName is "width" or "height" or "max-width")
        {
            return IsSafeLengthValue(propertyValue);
        }

        if (propertyName is "padding" or "padding-left" or "padding-right" or "padding-top" or "padding-bottom")
        {
            return IsSafeSpacingValue(propertyValue);
        }

        if (propertyName == "margin")
        {
            return IsSafeSpacingValue(propertyValue);
        }

        if (propertyName == "vertical-align")
        {
            return IsSafeVerticalAlign(propertyValue);
        }

        if (propertyName == "text-align")
        {
            return propertyValue is "left" or "center" or "right";
        }

        if (propertyName == "border-collapse")
        {
            return propertyValue is "collapse" or "separate";
        }

        if (propertyName == "table-layout")
        {
            return propertyValue is "auto" or "fixed";
        }

        return false;
    }

    private static bool IsSafeSpacingValue(string value)
    {
        return value
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Length is > 0 and <= 4
            && value
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .All(part => part == "auto" || IsSafeLengthValue(part));
    }

    private static bool IsSafeLengthValue(string value)
    {
        return LengthValueRegex().IsMatch(value);
    }

    private static bool IsSafeDimensionAttributeValue(string value)
    {
        return IsPositiveInteger(value) || IsSafeLengthValue(value);
    }

    private static bool IsSafeVerticalAlign(string value)
    {
        return value.Trim().ToLowerInvariant() is "top" or "middle" or "bottom" or "baseline";
    }

    private static string GetAttributeValue(Match attributeMatch)
    {
        if (attributeMatch.Groups["dq"].Success)
        {
            return attributeMatch.Groups["dq"].Value;
        }

        if (attributeMatch.Groups["sq"].Success)
        {
            return attributeMatch.Groups["sq"].Value;
        }

        return attributeMatch.Groups["bare"].Success
            ? attributeMatch.Groups["bare"].Value
            : string.Empty;
    }

    private static bool RequiresSafeUrl(string attributeName)
    {
        return attributeName is "href" or "src";
    }

    private static bool IsSafeUrl(string value)
    {
        var decodedValue = WebUtility.HtmlDecode(value).Trim();
        if (decodedValue.StartsWith('#') || decodedValue.StartsWith('/'))
        {
            return true;
        }

        if (!Uri.TryCreate(decodedValue, UriKind.RelativeOrAbsolute, out var uri))
        {
            return false;
        }

        if (!uri.IsAbsoluteUri)
        {
            return true;
        }

        return uri.Scheme is "http" or "https" or "mailto" or "tel";
    }

    private static bool IsPositiveInteger(string value)
    {
        return int.TryParse(value, out var number) && number > 0;
    }

    [GeneratedRegex("<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex CommentRegex();

    [GeneratedRegex("<\\s*(script|style|iframe|object|embed|svg|math|template|noscript|form)\\b[^>]*>.*?<\\s*/\\s*\\1\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex DangerousContentRegex();

    [GeneratedRegex("</?\\s*(script|style|iframe|object|embed|svg|math|template|noscript|form|input|button|textarea|select|option|link|meta|base|frame|frameset)\\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex DangerousTagRegex();

    [GeneratedRegex("<(?<closing>/)?\\s*(?<name>[a-zA-Z][a-zA-Z0-9:-]*)(?<attrs>[^<>]*?)(?<self>/?)>")]
    private static partial Regex TagRegex();

    [GeneratedRegex("(?<name>[a-zA-Z_:][a-zA-Z0-9_:\\.-]*)(?:\\s*=\\s*(?:\"(?<dq>[^\"]*)\"|'(?<sq>[^']*)'|(?<bare>[^\\s\"'=<>`]+)))?")]
    private static partial Regex AttributeRegex();

    [GeneratedRegex("^(?:0|\\d+(?:\\.\\d+)?(?:px|%|em|rem))$")]
    private static partial Regex LengthValueRegex();
}
