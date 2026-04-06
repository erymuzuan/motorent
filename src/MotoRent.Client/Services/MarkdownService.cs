using System.Net.Http;
using System.Threading.Tasks;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace MotoRent.Client.Services;

public class MarkdownService(HttpClient httpClient, NavigationManager navigationManager, ILogger<MarkdownService> logger)
{
    private HttpClient HttpClient { get; } = httpClient;
    private NavigationManager NavigationManager { get; } = navigationManager;
    private ILogger<MarkdownService> Logger { get; } = logger;
    private MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

    /// <summary>
    /// Gets the documentation folder path based on language.
    /// </summary>
    public static string GetDocsFolder(string lang) => lang == "ms" ? "user.guides.ms" : "user.guides";

    /// <summary>
    /// Renders a markdown file to HTML with optional language fallback.
    /// </summary>
    public async Task<string> RenderMarkdownAsync(string path)
    {
        try
        {
            var absoluteUri = this.NavigationManager.ToAbsoluteUri(path).ToString();
            var markdown = await this.HttpClient.GetStringAsync(absoluteUri);
            return Markdown.ToHtml(markdown, this.Pipeline);
        }
        catch (HttpRequestException)
        {
            return "<h1>Error</h1><p>Failed to load documentation file.</p>";
        }
    }

    /// <summary>
    /// Renders a markdown document with language awareness and fallback to English.
    /// </summary>
    public async Task<(string Html, bool UsedFallback)> RenderMarkdownWithFallbackAsync(string fileName, string lang)
    {
        var docsFolder = GetDocsFolder(lang);
        var path = $"{docsFolder}/{fileName}";

        try
        {
            var absoluteUri = this.NavigationManager.ToAbsoluteUri(path).ToString();
            var markdown = await this.HttpClient.GetStringAsync(absoluteUri);
            return (Markdown.ToHtml(markdown, this.Pipeline), false);
        }
        catch (HttpRequestException ex)
        {
            // If Malay doesn't exist, fall back to English
            if (lang == "ms")
            {
                this.Logger.LogInformation("Malay version not found for {FileName}, falling back to English", fileName);
                var englishPath = $"user.guides/{fileName}";
                try
                {
                    var absoluteUri = this.NavigationManager.ToAbsoluteUri(englishPath).ToString();
                    var markdown = await this.HttpClient.GetStringAsync(absoluteUri);
                    return (Markdown.ToHtml(markdown, this.Pipeline), true);
                }
                catch (HttpRequestException)
                {
                    this.Logger.LogError("English fallback also failed for {FileName}", fileName);
                    return ("<h1>Error</h1><p>Failed to load documentation file.</p>", false);
                }
            }

            this.Logger.LogError(ex, "Failed to load document: {Path}", path);
            return ("<h1>Error</h1><p>Failed to load documentation file.</p>", false);
        }
    }

    public string ToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown, this.Pipeline);
    }
}

public record DocItem(string Title, string FileName, int Order);
