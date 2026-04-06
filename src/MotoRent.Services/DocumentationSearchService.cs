using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;
using MotoRent.Domain.Extensions;

namespace MotoRent.Services;

public class DocumentationSearchService(
    IHttpClientFactory httpClientFactory,
    ILogger<DocumentationSearchService> logger,
    string docsPath)
{
    private IHttpClientFactory HttpClientFactory { get; } = httpClientFactory;
    private ILogger<DocumentationSearchService> Logger { get; } = logger;
    private string DocsPath { get; } = docsPath;

    public async Task<GeminiSearchResult> AskGeminiAsync(string question, CancellationToken cancellationToken = default)
    {
        var apiKey = MotoConfig.GeminiApiKey;

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new GeminiSearchResult("Gemini API key is not configured.", "", 0, 0, false, "No API key");
        }

        var context = await this.GetDocumentationContextAsync();

        if (string.IsNullOrWhiteSpace(context))
        {
            return new GeminiSearchResult(
                "I'm sorry, I couldn't find information about that in our guides.",
                "", 0, 0, false, "No documentation context");
        }

        var request = CreateGeminiRequest(context, question);
        var client = this.HttpClientFactory.CreateClient("Gemini");
        Exception? lastException = null;

        foreach (var model in MotoConfig.GeminiModels)
        {
            try
            {
                using var httpRequest = CreateGeminiHttpRequest(apiKey, model, request);
                using var response = await client.SendAsync(httpRequest, cancellationToken);
                var responseBody = await response.ReadContentAsStringAsync(false);

                if (!response.IsSuccessStatusCode)
                {
                    lastException = new HttpRequestException(
                        $"Gemini model {model} returned HTTP {(int)response.StatusCode}: {responseBody}");
                    this.Logger.LogWarning(
                        "Gemini request failed for model {Model} with HTTP {StatusCode}",
                        model,
                        (int)response.StatusCode);
                    continue;
                }

                var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseBody);
                var answer = geminiResponse?.Candidates?
                    .FirstOrDefault()?
                    .Content?
                    .Parts?
                    .Select(x => x.Text)
                    .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

                if (!string.IsNullOrWhiteSpace(answer))
                {
                    var usage = geminiResponse?.UsageMetadata;
                    return new GeminiSearchResult(
                        answer,
                        model,
                        usage?.PromptTokenCount ?? 0,
                        usage?.CandidatesTokenCount ?? 0,
                        true,
                        null);
                }

                this.Logger.LogWarning("Gemini returned an empty answer for model {Model}", model);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                this.Logger.LogWarning(ex, "Gemini request failed for model {Model}", model);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = ex;
                this.Logger.LogWarning(ex, "Gemini request timed out for model {Model}", model);
            }
            catch (JsonException ex)
            {
                lastException = ex;
                this.Logger.LogWarning(ex, "Failed to parse Gemini response for model {Model}", model);
            }
        }

        this.Logger.LogError(lastException, "Failed to get response from Gemini API for documentation search");
        return new GeminiSearchResult(
            "An error occurred while communicating with the AI assistant.",
            MotoConfig.GeminiModels.FirstOrDefault() ?? "",
            0, 0, false, lastException?.Message);
    }

    private async Task<string> GetDocumentationContextAsync()
    {
        this.Logger.LogInformation("Gathering documentation context from: {Path}", this.DocsPath);

        if (!Directory.Exists(this.DocsPath))
        {
            this.Logger.LogWarning("Documentation path does not exist: {Path}", this.DocsPath);
            return string.Empty;
        }

        var files = Directory.GetFiles(this.DocsPath, "*.md");
        this.Logger.LogInformation("Found {Count} documentation files.", files.Length);

        var contextBuilder = new System.Text.StringBuilder();

        foreach (var file in files)
        {
            if (Path.GetFileName(file) == "AUDIT.md" || Path.GetFileName(file) == "README.md") continue;

            var content = await File.ReadAllTextAsync(file);
            contextBuilder.AppendLine($"--- File: {Path.GetFileName(file)} ---");
            contextBuilder.AppendLine(content);
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }

    private static object CreateGeminiRequest(string context, string question)
    {
        return new
        {
            system_instruction = new
            {
                parts = new[]
                {
                    new
                    {
                        text = """
                            You are the MotoRent Help Assistant.
                            Answer using only the provided documentation context.
                            If the answer is not supported by the documentation, say: "I'm sorry, I couldn't find information about that in our guides."
                            Provide clear step-by-step guidance when it helps.
                            Match the user's language when possible, including English, Thai, or Bahasa Melayu.
                            End every answer with sources in this exact format: Sources: [filename1, filename2]
                            Only cite files that are present in the provided context.
                            """
                    }
                }
            },
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new
                        {
                            text = $"""
                                Documentation Context:
                                {context}

                                User Question:
                                {question}
                                """
                        }
                    }
                }
            },
            generationConfig = new
            {
                maxOutputTokens = 2048,
                thinkingConfig = new
                {
                    thinkingLevel = "low"
                }
            }
        };
    }

    private static HttpRequestMessage CreateGeminiHttpRequest(string apiKey, string model, object request)
    {
        var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent");
        httpRequest.Headers.Add("x-goog-api-key", apiKey);
        httpRequest.Content = JsonContent.Create(request);
        return httpRequest;
    }
}

public record GeminiSearchResult(
    string Answer,
    string Model,
    int InputTokens,
    int OutputTokens,
    bool Success,
    string? Error);
