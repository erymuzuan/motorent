using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;

namespace MotoRent.Services;

public class DocumentationTranslationService(
    IHttpClientFactory httpClientFactory,
    ILogger<DocumentationTranslationService> logger,
    string docsPath)
{
    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    private ILogger<DocumentationTranslationService> Logger { get; } = logger;
    private string DocsPath { get; } = docsPath;
    private string ThaiDocsPath => Path.Combine(Path.GetDirectoryName(this.DocsPath)!, "user.guides.th");

    /// <summary>
    /// Translates a markdown document to Thai using Gemini API.
    /// </summary>
    public async Task<TranslationResult> TranslateDocumentAsync(
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var apiKey = MotoConfig.GeminiApiKey;
        var model = MotoConfig.GeminiModel;

        if (string.IsNullOrEmpty(apiKey))
        {
            return new TranslationResult(false, "Gemini API key is not configured.", null);
        }

        var sourcePath = Path.Combine(this.DocsPath, fileName);
        if (!File.Exists(sourcePath))
        {
            return new TranslationResult(false, $"Source file not found: {fileName}", null);
        }

        var sourceContent = await File.ReadAllTextAsync(sourcePath, cancellationToken);
        var sourceHash = ComputeHash(sourceContent);

        this.Logger.LogInformation("Translating {FileName} to Thai (hash: {Hash})", fileName, sourceHash[..16]);

        var prompt = BuildTranslationPrompt(sourceContent);

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
                temperature = 0.3,
                maxOutputTokens = 8192
            }
        };

        var client = this.HttpClientFactory.CreateClient("Gemini");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";

        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("x-goog-api-key", apiKey);
            httpRequest.Content = JsonContent.Create(request);
            var response = await client.SendAsync(httpRequest, cancellationToken);
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken);
            var translatedContent = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(translatedContent))
            {
                return new TranslationResult(false, "Gemini returned empty translation.", null);
            }

            // Clean up any markdown code fence wrapper if present
            translatedContent = CleanMarkdownWrapper(translatedContent);

            // Save the translated file
            var targetPath = Path.Combine(this.ThaiDocsPath, fileName);
            await File.WriteAllTextAsync(targetPath, translatedContent, cancellationToken);

            // Update meta.json
            await this.UpdateMetaJsonAsync(fileName, sourceHash, cancellationToken);

            this.Logger.LogInformation("Successfully translated {FileName} to Thai", fileName);

            return new TranslationResult(true, null, translatedContent);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to translate {FileName}", fileName);
            return new TranslationResult(false, $"Translation error: {ex.Message}", null);
        }
    }

    /// <summary>
    /// Gets the translation status for all documents.
    /// </summary>
    public async Task<List<DocumentTranslationStatus>> GetTranslationStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var metaPath = Path.Combine(this.DocsPath, "meta.json");
        if (!File.Exists(metaPath))
        {
            return [];
        }

        var metaJson = await File.ReadAllTextAsync(metaPath, cancellationToken);
        var meta = JsonSerializer.Deserialize<MetaJson>(metaJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var statuses = new List<DocumentTranslationStatus>();

        foreach (var doc in meta?.Documents ?? new Dictionary<string, DocumentMeta>())
        {
            var thaiFilePath = Path.Combine(this.ThaiDocsPath, doc.Key);
            var thaiExists = File.Exists(thaiFilePath);
            var thStatus = doc.Value.Translations?.GetValueOrDefault("th");

            statuses.Add(new DocumentTranslationStatus(
                doc.Key,
                doc.Value.ContentHash ?? string.Empty,
                thaiExists,
                thStatus?.Status ?? "pending",
                thStatus?.SourceHash == doc.Value.ContentHash
            ));
        }

        return statuses.OrderBy(s => s.FileName).ToList();
    }

    /// <summary>
    /// Batch translates all pending documents.
    /// </summary>
    public async Task<BatchTranslationResult> TranslateAllPendingAsync(
        CancellationToken cancellationToken = default)
    {
        var statuses = await this.GetTranslationStatusAsync(cancellationToken);
        var pending = statuses.Where(s => s.Status == "pending" || !s.IsCurrent).ToList();

        var results = new List<(string FileName, bool Success, string? Error)>();

        foreach (var doc in pending)
        {
            var result = await this.TranslateDocumentAsync(doc.FileName, cancellationToken);
            results.Add((doc.FileName, result.Success, result.Error));

            if (!result.Success)
            {
                this.Logger.LogWarning("Failed to translate {FileName}: {Error}", doc.FileName, result.Error);
            }

            // Small delay to avoid rate limiting
            await Task.Delay(1000, cancellationToken);
        }

        return new BatchTranslationResult(
            results.Count(r => r.Success),
            results.Count(r => !r.Success),
            results
        );
    }

    private async Task UpdateMetaJsonAsync(string fileName, string sourceHash, CancellationToken cancellationToken)
    {
        var metaPath = Path.Combine(this.DocsPath, "meta.json");
        if (!File.Exists(metaPath)) return;

        var metaJson = await File.ReadAllTextAsync(metaPath, cancellationToken);
        using var doc = JsonDocument.Parse(metaJson);

        var root = doc.RootElement;
        var newMeta = new Dictionary<string, object>();

        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name == "documents")
            {
                var documents = new Dictionary<string, object>();
                foreach (var docProp in prop.Value.EnumerateObject())
                {
                    if (docProp.Name == fileName)
                    {
                        var docMeta = JsonSerializer.Deserialize<Dictionary<string, object>>(docProp.Value.GetRawText())!;
                        var translations = new Dictionary<string, object>
                        {
                            ["th"] = new { sourceHash = sourceHash[..16], status = "current" }
                        };
                        docMeta["translations"] = translations;
                        documents[docProp.Name] = docMeta;
                    }
                    else
                    {
                        documents[docProp.Name] = JsonSerializer.Deserialize<object>(docProp.Value.GetRawText())!;
                    }
                }
                newMeta["documents"] = documents;
            }
            else if (prop.Name == "lastUpdated")
            {
                newMeta["lastUpdated"] = DateTime.UtcNow.ToString("O");
            }
            else
            {
                newMeta[prop.Name] = JsonSerializer.Deserialize<object>(prop.Value.GetRawText())!;
            }
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        var updatedJson = JsonSerializer.Serialize(newMeta, options);
        await File.WriteAllTextAsync(metaPath, updatedJson, cancellationToken);
    }

    private static string BuildTranslationPrompt(string sourceContent)
    {
        return $"""
            You are a professional translator. Translate the following English markdown documentation to Thai.

            TRANSLATION RULES:
            1. Keep ALL markdown formatting intact (headers, lists, code blocks, tables, links, images)
            2. Keep these terms in English:
               - Brand names: MotoRent
               - Technical terms: API, URL, JSON, HTML, CSS, ID, GPS, PDF, etc.
               - File paths and extensions: .md, .json, .png, etc.
               - Code snippets and examples
               - Image paths (e.g., images/xxx.png)
               - Internal links to .md files
            3. Translate these naturally to Thai:
               - Menu names and UI labels
               - Instructions and descriptions
               - Headers and section titles
               - Button labels (use transliterations where appropriate)
            4. Use common Thai transliterations for technical terms:
               - Check-In → เช็คอิน
               - Check-Out → เช็คเอาท์
               - Dashboard → แดชบอร์ด
               - Login → ล็อกอิน
               - Logout → ล็อกเอาท์
               - Settings → การตั้งค่า
               - Profile → โปรไฟล์
            5. Maintain the same document structure and layout
            6. Keep frontmatter (if any) intact
            7. Output ONLY the translated markdown content, no explanations

            SOURCE DOCUMENT:
            {sourceContent}

            TRANSLATED DOCUMENT (Thai):
            """;
    }

    private static string CleanMarkdownWrapper(string content)
    {
        content = content.Trim();

        // Remove markdown code fence if present
        if (content.StartsWith("```markdown") || content.StartsWith("```md"))
        {
            var firstNewline = content.IndexOf('\n');
            if (firstNewline > 0)
            {
                content = content[(firstNewline + 1)..];
            }
        }
        else if (content.StartsWith("```"))
        {
            var firstNewline = content.IndexOf('\n');
            if (firstNewline > 0)
            {
                content = content[(firstNewline + 1)..];
            }
        }

        if (content.EndsWith("```"))
        {
            content = content[..^3].TrimEnd();
        }

        return content;
    }

    private static string ComputeHash(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}

public record TranslationResult(bool Success, string? Error, string? TranslatedContent);

public record DocumentTranslationStatus(
    string FileName,
    string ContentHash,
    bool ThaiFileExists,
    string Status,
    bool IsCurrent);

public record BatchTranslationResult(
    int SuccessCount,
    int FailureCount,
    List<(string FileName, bool Success, string? Error)> Results);

internal class MetaJson
{
    public string? Version { get; set; }
    public string? LastUpdated { get; set; }
    public Dictionary<string, DocumentMeta>? Documents { get; set; }
}

internal class DocumentMeta
{
    public string? ContentHash { get; set; }
    public string? LastModified { get; set; }
    public Dictionary<string, TranslationMeta>? Translations { get; set; }
}

internal class TranslationMeta
{
    public string? SourceHash { get; set; }
    public string? Status { get; set; }
}
