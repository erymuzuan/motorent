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
            Convert Thai company name to English identifier. Follow these rules EXACTLY:

            1. บริษัท → "Co" (place after brand name)
            2. จำกัด → "Ltd" (place at end)
            3. ห้างหุ้นส่วน → "Partnership"
            4. (มหาชน) → "Plc"
            5. All other Thai words → transliterate phonetically to English
            6. Output format: PascalCase, no spaces, no punctuation
            7. Return ONLY the final identifier, nothing else

            Examples:
            - "บริษัท สตาฟี จำกัด" → "StaffyCoLtd"
            - "บริษัท อาดัม มันนี่ จำกัด" → "AdamMoneyCoLtd"
            - "บริษัท โมโตเรนท์ จำกัด" → "MotorentCoLtd"
            - "บริษัท ไทยพาณิชย์ จำกัด (มหาชน)" → "ThaiPanitCoLtdPlc"
            - "ห้างหุ้นส่วน สยาม" → "SiamPartnership"
            - "Phuket Motor Rentals Co., Ltd." → "PhuketMotorRentalsCoLtd"

            Input: {text}
            Output:
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
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        try
        {
            var response = await client.PostAsJsonAsync(url, request, s_jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(s_jsonOptions, cancellationToken);
            var result = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text?.Trim() ?? text;

            // Sanitize: keep only ASCII letters and numbers
            result = SanitizeRegex().Replace(result, "");

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
