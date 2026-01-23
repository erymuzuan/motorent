using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;

namespace MotoRent.Services;

public class DocumentTemplateAiService(
    IHttpClientFactory httpClientFactory,
    ILogger<DocumentTemplateAiService> logger)
{
    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    private ILogger<DocumentTemplateAiService> Logger { get; } = logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true
    };

    public async Task<List<string>> GetSuggestedClausesAsync(
        DocumentType type,
        string context,
        CancellationToken cancellationToken = default)
    {
        var apiKey = MotoConfig.GeminiApiKey;
        var model = MotoConfig.GeminiModel;

        if (string.IsNullOrEmpty(apiKey))
        {
            this.Logger.LogWarning("Gemini API key not configured, returning empty suggestions");
            return new List<string>();
        }

        var prompt = $"""
            You are a professional legal assistant for a motorbike rental business in Thailand.
            Suggest 5-8 common clauses for a {type} document.
            Context: {context}
            
            Return ONLY a valid JSON array of strings, where each string is a clause.
            Do not include markdown formatting or extra text.
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
                temperature = 0.7,
                maxOutputTokens = 2048,
                responseMimeType = "application/json"
            }
        };

        var client = this.HttpClientFactory.CreateClient("Gemini");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        try
        {
            var response = await client.PostAsJsonAsync(url, request, s_jsonOptions, cancellationToken);
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(s_jsonOptions, cancellationToken);
            var rawJson = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "[]";

            // Clean up the JSON response (remove markdown code blocks if present)
            rawJson = rawJson.Trim();
            if (rawJson.StartsWith("```json")) rawJson = rawJson[7..];
            if (rawJson.StartsWith("```")) rawJson = rawJson[3..];
            if (rawJson.EndsWith("```")) rawJson = rawJson[..^3];
            rawJson = rawJson.Trim();

            return JsonSerializer.Deserialize<List<string>>(rawJson, s_jsonOptions) ?? new List<string>();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get AI suggested clauses from Gemini API");
            return new List<string> { "Error generating suggestions: " + ex.Message };
        }
    }
}
