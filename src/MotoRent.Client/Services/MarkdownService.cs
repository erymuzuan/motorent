using System.Net.Http;
using System.Threading.Tasks;
using Markdig;

namespace MotoRent.Client.Services;

public class MarkdownService
{
    private readonly HttpClient m_httpClient;
    private readonly MarkdownPipeline m_pipeline;

    public MarkdownService(HttpClient httpClient)
    {
        m_httpClient = httpClient;
        m_pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public async Task<string> RenderMarkdownAsync(string path)
    {
        try
        {
            var markdown = await m_httpClient.GetStringAsync(path);
            return Markdown.ToHtml(markdown, m_pipeline);
        }
        catch (HttpRequestException)
        {
            return "<h1>Error</h1><p>Failed to load documentation file.</p>";
        }
    }

    public string ToHtml(string markdown)
    {
        return Markdown.ToHtml(markdown, m_pipeline);
    }
}

public record DocItem(string Title, string FileName, int Order);
