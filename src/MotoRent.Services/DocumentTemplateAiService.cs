using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Models;

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

    private static readonly JsonSerializerOptions s_layoutDeserializeOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
            return [];
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

            return JsonSerializer.Deserialize<List<string>>(rawJson, s_jsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get AI suggested clauses from Gemini API");
            return ["Error generating suggestions: " + ex.Message];
        }
    }

    public async Task<DocumentLayout?> ExtractTemplateLayoutAsync(
        byte[] fileBytes,
        string mimeType,
        DocumentType documentType,
        CancellationToken cancellationToken = default)
    {
        var apiKey = MotoConfig.GeminiApiKey;
        var model = MotoConfig.GeminiModel;

        if (string.IsNullOrEmpty(apiKey))
        {
            this.Logger.LogWarning("Gemini API key not configured for template extraction");
            return null;
        }

        var base64Data = Convert.ToBase64String(fileBytes);

        var placeholdersByType = documentType switch
        {
            DocumentType.BookingConfirmation =>
                "Booking: {{Booking.Ref}}, {{Booking.CustomerName}}, {{Booking.CustomerPhone}}, {{Booking.CustomerEmail}}, " +
                "{{Booking.CustomerPassport}}, {{Booking.CustomerNationality}}, {{Booking.HotelName}}, " +
                "{{Booking.StartDate}}, {{Booking.EndDate}}, {{Booking.TotalAmount}}, {{Booking.DepositRequired}}, " +
                "{{Booking.AmountPaid}}, {{Booking.BalanceDue}}, {{Booking.Status}}, {{Booking.Notes}}, {{Booking.Days}}",
            DocumentType.RentalAgreement =>
                "Rental: {{Rental.Id}}, {{Rental.ContractNo}}, {{Rental.CustomerName}}, {{Rental.StartDate}}, " +
                "{{Rental.EndDate}}, {{Rental.Status}}, {{Rental.TotalAmount}}, {{Rental.VehicleName}}, " +
                "{{Rental.Days}}, {{Rental.BalanceDue}}",
            DocumentType.Receipt =>
                "Receipt: {{Receipt.Id}}, {{Receipt.No}}, {{Receipt.Date}}, {{Receipt.TotalAmount}}, " +
                "{{Receipt.CustomerName}}, {{Receipt.Status}}",
            _ => ""
        };

        var systemInstruction = """
            You are a document layout extraction AI for a motorbike rental business in Thailand.
            Analyze the uploaded document and extract its visual layout into structured JSON blocks.

            Available block types and their JSON schemas:
            1. "heading" - { "$type": "heading", "content": "text", "level": 1-6, "horizontalAlignment": "Left|Center|Right", "isBold": true/false }
            2. "text" - { "$type": "text", "content": "text", "horizontalAlignment": "Left|Center|Right", "isBold": true/false }
            3. "table" - { "$type": "table", "columns": [{ "header": "col name", "bindingPath": "field" }] }
            4. "image" - { "$type": "image", "bindingPath": "Organization.Logo", "width": 200 }
            5. "divider" - { "$type": "divider", "thickness": 1, "color": "#000000" }
            6. "signature" - { "$type": "signature", "label": "Signature label" }
            7. "two-columns" - { "$type": "two-columns", "leftColumn": [...blocks], "rightColumn": [...blocks] }
            8. "spacer" - { "$type": "spacer", "height": 20 }

            Available placeholder tokens for DOCUMENT_TYPE documents:
            Organization: {{Org.Name}}, {{Org.Email}}, {{Org.Phone}}, {{Org.Address}}, {{Org.TaxId}}, {{Org.Website}}
            Staff: {{Staff.Name}}, {{Staff.UserName}}
            PLACEHOLDER_TOKENS

            IMPORTANT RULES:
            - Replace actual data values with the closest matching {{Placeholder}} token
            - Keep static labels and clause text as-is
            - Use "two-columns" for side-by-side content like label:value pairs
            - Use "heading" for titles and section headers
            - Use "divider" between major sections
            - Use "signature" for signature lines
            - Use "spacer" for vertical spacing
            - For company logos, use image block with bindingPath "Organization.Logo"

            Return a JSON object with this structure:
            {
              "sections": [
                {
                  "name": "Section Name",
                  "blocks": [ ...block objects with $type discriminator... ]
                }
              ]
            }
            """;

        systemInstruction = systemInstruction
            .Replace("DOCUMENT_TYPE", documentType.ToString())
            .Replace("PLACEHOLDER_TOKENS", placeholdersByType);

        var request = new
        {
            systemInstruction = new
            {
                parts = new[] { new { text = systemInstruction } }
            },
            contents = new[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { inlineData = new { mimeType, data = base64Data } },
                        new { text = $"Extract the layout of this {documentType} document into template blocks." }
                    }
                }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 8192,
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
            var rawJson = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                this.Logger.LogWarning("Gemini returned empty response for template extraction");
                return null;
            }

            rawJson = rawJson.Trim();
            if (rawJson.StartsWith("```json")) rawJson = rawJson[7..];
            if (rawJson.StartsWith("```")) rawJson = rawJson[3..];
            if (rawJson.EndsWith("```")) rawJson = rawJson[..^3];
            rawJson = rawJson.Trim();

            return JsonSerializer.Deserialize<DocumentLayout>(rawJson, s_layoutDeserializeOptions);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to extract template layout from document via Gemini API");
            return null;
        }
    }
}
