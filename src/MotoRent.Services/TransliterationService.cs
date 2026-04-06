using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;

namespace MotoRent.Services;

/// <summary>
/// Service for transliterating non-English text to English using Gemini API.
/// </summary>
public partial class TransliterationService(
    IHttpClientFactory httpClientFactory,
    ILogger<TransliterationService> logger)
{
    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    private ILogger<TransliterationService> Logger { get; } = logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Transliterates text to English. If text is already English, returns as-is.
    /// </summary>
    public async Task<string> TransliterateToEnglishAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // Check if text contains non-ASCII characters
        if (!ContainsNonAscii(text))
            return text;

        var apiKey = MotoConfig.GeminiApiKey;
        var model = MotoConfig.GeminiModel;

        if (string.IsNullOrEmpty(apiKey))
        {
            this.Logger.LogWarning("Gemini API key not configured, returning original text");
            return text;
        }

        var prompt = $"""
            Convert Thai company name to English identifier.

            CRITICAL: Output MUST be PascalCase - EVERY word starts with UPPERCASE letter.
            - "Co" not "co", "Ltd" not "ltd", "Plc" not "plc"

            Rules:
            1. บริษัท → "Co" (place after brand name)
            2. จำกัด → "Ltd" (at end)
            3. ห้างหุ้นส่วน → "Partnership"
            4. (มหาชน) → "Plc"
            5. Thai words → transliterate phonetically, capitalize first letter
            6. NO spaces, NO punctuation
            7. Return ONLY the identifier

            Examples (note the capitalization):
            - "บริษัท สตาฟี จำกัด" → "StaffyCoLtd"
            - "บริษัท อาดัม มันนี่ จำกัด" → "AdamMoneyCoLtd"
            - "ห้างหุ้นส่วน สยาม" → "SiamPartnership"
            - "Phuket Motor Rentals" → "PhuketMotorRentals"

            Input: {text}
            """;

        var request = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.1,
                maxOutputTokens = 100
            }
        };

        var client = this.HttpClientFactory.CreateClient("Gemini");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("x-goog-api-key", apiKey);
            httpRequest.Content = JsonContent.Create(request, options: s_jsonOptions);
            var response = await client.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(s_jsonOptions, cancellationToken);
            var result = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim() ?? text;

            // Sanitize: keep only ASCII letters and numbers
            result = SanitizeRegex().Replace(result, "");

            // Fix common suffixes that Gemini might not capitalize correctly
            result = FixCompanySuffixes(result);

            this.Logger.LogInformation("Transliterated '{Original}' to '{Result}'", text, result);

            return string.IsNullOrWhiteSpace(result) ? text : result;
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to transliterate text '{Text}'", text);
            return text;
        }
    }

    private static bool ContainsNonAscii(string text)
    {
        return text.Any(c => c > 127);
    }

    /// <summary>
    /// Fix common company suffixes to proper PascalCase.
    /// Handles cases where Gemini returns lowercase suffixes.
    /// </summary>
    private static string FixCompanySuffixes(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Fix common patterns (case-insensitive replacement at end or before other suffixes)
        // Order matters: longer patterns first
        var replacements = new (string pattern, string replacement)[]
        {
            ("coltdplc", "CoLtdPlc"),
            ("coltd", "CoLtd"),
            ("colimited", "CoLimited"),
            ("partnership", "Partnership"),
            ("ltd", "Ltd"),
            ("plc", "Plc"),
        };

        foreach (var (pattern, replacement) in replacements)
        {
            var index = text.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (index > 0) // Must have something before the suffix
            {
                var before = text[..index];
                var after = text[(index + pattern.Length)..];

                // Ensure first letter of brand name is uppercase
                if (before.Length > 0 && char.IsLower(before[0]))
                {
                    before = char.ToUpper(before[0]) + before[1..];
                }

                text = before + replacement + after;
                break; // Only fix one pattern to avoid double-fixing
            }
        }

        // Ensure first character is uppercase
        if (text.Length > 0 && char.IsLower(text[0]))
        {
            text = char.ToUpper(text[0]) + text[1..];
        }

        return text;
    }

    [GeneratedRegex(@"[^A-Za-z0-9]")]
    private static partial Regex SanitizeRegex();

    // Gemini API response classes
    private class GeminiResponse
    {
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        public Content? Content { get; set; }
    }

    private class Content
    {
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        public string? Text { get; set; }
    }
}
