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
        "code",
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
        "th",
        "thead",
        "tr",
        "u",
        "ul"
    };

    private static readonly ISet<string> VoidTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "br",
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
                "title",
                "width"
            },
            ["td"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "colspan",
                "rowspan"
            },
            ["th"] = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "colspan",
                "rowspan"
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
                || attributeName.Equals("style", StringComparison.OrdinalIgnoreCase)
                || attributeName.Equals("srcdoc", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var attributeValue = GetAttributeValue(attributeMatch);
            if (RequiresSafeUrl(attributeName) && !IsSafeUrl(attributeValue))
            {
                continue;
            }

            if (attributeName is "width" or "height" or "colspan" or "rowspan"
                && !IsPositiveInteger(attributeValue))
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
}
