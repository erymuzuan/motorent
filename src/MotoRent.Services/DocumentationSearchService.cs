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

namespace MotoRent.Services;

public class DocumentationSearchService
{
    private readonly IHttpClientFactory m_httpClientFactory;
    private readonly ILogger<DocumentationSearchService> m_logger;
    private readonly string m_docsPath;

    public DocumentationSearchService(
        IHttpClientFactory httpClientFactory,
        ILogger<DocumentationSearchService> logger,
        string docsPath)
    {
        m_httpClientFactory = httpClientFactory;
        m_logger = logger;
        m_docsPath = docsPath;
    }

    public async Task<string> AskGeminiAsync(string question, CancellationToken cancellationToken = default)
    {
        var apiKey = MotoConfig.GeminiApiKey;
        var model = MotoConfig.GeminiModel;

        if (string.IsNullOrEmpty(apiKey))
        {
            return "Gemini API key is not configured.";
        }

        var context = await GetDocumentationContextAsync();
        var prompt = $"""
            You are the MotoRent Help Assistant. Use the following documentation context to answer the user's question.
            If the answer is not in the documentation, say "I'm sorry, I couldn't find information about that in our guides."
            Provide clear, step-by-step instructions when applicable.
            You can answer in English or Thai, matching the user's language.

            CRITICAL: At the end of your answer, list the files you used as sources in the format: "Sources: [filename1, filename2]".

            Documentation Context:
            {context}

            User Question: {question}

            Answer:
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
                temperature = 0.2,
                maxOutputTokens = 2048
            }
        };

        var client = m_httpClientFactory.CreateClient("Gemini");
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        try
        {
            var response = await client.PostAsJsonAsync(url, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var geminiResponse = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken);
            return geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text 
                   ?? "I'm sorry, I couldn't generate a response.";
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Failed to get response from Gemini API for documentation search");
            return "An error occurred while communicating with the AI assistant.";
        }
    }

    private async Task<string> GetDocumentationContextAsync()
    {
        m_logger.LogInformation("Gathering documentation context from: {Path}", m_docsPath);

        if (!Directory.Exists(m_docsPath))
        {
            m_logger.LogWarning("Documentation path does not exist: {Path}", m_docsPath);
            return string.Empty;
        }

        var files = Directory.GetFiles(m_docsPath, "*.md");
        m_logger.LogInformation("Found {Count} documentation files.", files.Length);

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
}