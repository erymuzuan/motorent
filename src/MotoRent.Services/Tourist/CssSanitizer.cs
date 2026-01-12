using System.Text.RegularExpressions;

namespace MotoRent.Services.Tourist;

/// <summary>
/// Sanitizes custom CSS to prevent XSS attacks and other security issues.
/// </summary>
public static partial class CssSanitizer
{
    // Dangerous patterns that could be used for XSS or data exfiltration
    private static readonly string[] s_dangerousPatterns =
    [
        @"javascript\s*:",           // javascript: URLs
        @"expression\s*\(",          // IE expression()
        @"behavior\s*:",             // IE behavior
        @"-moz-binding\s*:",         // Firefox XBL binding
        @"@import",                  // External CSS imports
        @"@charset",                 // Charset declaration
        @"url\s*\(\s*['""]?\s*data\s*:", // data: URLs (can contain scripts)
        @"url\s*\(\s*['""]?\s*javascript\s*:", // javascript: in url()
        @"\\00",                     // Unicode escape sequences
        @"\\u00",                    // Unicode escape sequences
        @"&\s*#",                    // HTML entities
        @"<\s*script",               // Script tags
        @"<\s*style",                // Embedded style tags
        @"<\s*link",                 // Link tags
        @"<\s*meta",                 // Meta tags
        @"<\s*iframe",               // Iframe tags
        @"<\s*object",               // Object tags
        @"<\s*embed",                // Embed tags
        @"<\s*form",                 // Form tags
        @"<\s*input",                // Input tags
        @"<\s*img\s+[^>]*onerror",   // img onerror
        @"on\w+\s*=",                // Event handlers (onclick, onerror, etc.)
    ];

    // Allowed URL schemes for url() function
    private static readonly string[] s_allowedUrlSchemes = ["https://", "http://", "/", "./", "../"];

    /// <summary>
    /// Sanitizes custom CSS input by removing dangerous patterns.
    /// </summary>
    /// <param name="css">The CSS string to sanitize</param>
    /// <returns>Sanitized CSS string, or empty string if input is null/empty</returns>
    public static string Sanitize(string? css)
    {
        if (string.IsNullOrWhiteSpace(css))
            return string.Empty;

        var sanitized = css;

        // Remove dangerous patterns
        foreach (var pattern in s_dangerousPatterns)
        {
            sanitized = Regex.Replace(
                sanitized,
                pattern,
                string.Empty,
                RegexOptions.IgnoreCase | RegexOptions.Multiline);
        }

        // Validate and sanitize url() functions
        sanitized = SanitizeUrls(sanitized);

        // Remove any remaining HTML-like content
        sanitized = Regex.Replace(sanitized, @"<[^>]*>", string.Empty);

        // Remove null bytes
        sanitized = sanitized.Replace("\0", string.Empty);

        // Limit length to prevent DoS
        const int maxLength = 50000;
        if (sanitized.Length > maxLength)
            sanitized = sanitized[..maxLength];

        return sanitized.Trim();
    }

    /// <summary>
    /// Validates if CSS is safe (contains no dangerous patterns).
    /// </summary>
    public static bool IsSafe(string? css)
    {
        if (string.IsNullOrWhiteSpace(css))
            return true;

        foreach (var pattern in s_dangerousPatterns)
        {
            if (Regex.IsMatch(css, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a list of security issues found in the CSS.
    /// </summary>
    public static List<string> GetSecurityIssues(string? css)
    {
        var issues = new List<string>();

        if (string.IsNullOrWhiteSpace(css))
            return issues;

        if (Regex.IsMatch(css, @"javascript\s*:", RegexOptions.IgnoreCase))
            issues.Add("Contains javascript: URL");

        if (Regex.IsMatch(css, @"expression\s*\(", RegexOptions.IgnoreCase))
            issues.Add("Contains CSS expression()");

        if (Regex.IsMatch(css, @"@import", RegexOptions.IgnoreCase))
            issues.Add("Contains @import rule (external CSS not allowed)");

        if (Regex.IsMatch(css, @"url\s*\(\s*['""]?\s*data\s*:", RegexOptions.IgnoreCase))
            issues.Add("Contains data: URL");

        if (Regex.IsMatch(css, @"on\w+\s*=", RegexOptions.IgnoreCase))
            issues.Add("Contains event handler attributes");

        if (Regex.IsMatch(css, @"<\s*script", RegexOptions.IgnoreCase))
            issues.Add("Contains script tags");

        return issues;
    }

    private static string SanitizeUrls(string css)
    {
        // Match url() functions and validate their content
        return UrlRegex().Replace(css, match =>
        {
            var url = match.Groups[1].Value.Trim().Trim('"', '\'');

            // Check if URL starts with an allowed scheme
            foreach (var scheme in s_allowedUrlSchemes)
            {
                if (url.StartsWith(scheme, StringComparison.OrdinalIgnoreCase))
                    return match.Value; // Keep the original
            }

            // Remove URLs with disallowed schemes
            return "url('')";
        });
    }

    [GeneratedRegex(@"url\s*\(\s*([^)]+)\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();
}
