using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using MotoRent.Domain.Core;
using MotoRent.Services;
using Xunit;

namespace MotoRent.Server.Tests;

public class DocumentationSearchServiceTests
{
    private readonly Mock<IHttpClientFactory> m_httpClientFactoryMock;
    private readonly Mock<ILogger<DocumentationSearchService>> m_loggerMock;
    private readonly string m_testDocsPath;

    public DocumentationSearchServiceTests()
    {
        m_httpClientFactoryMock = new Mock<IHttpClientFactory>();
        m_loggerMock = new Mock<ILogger<DocumentationSearchService>>();
        m_testDocsPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(m_testDocsPath);
    }

    [Fact]
    public async Task AskGeminiAsync_ShouldIncludeDocContextInPrompt()
    {
        // Arrange
        var docsDir = Path.Combine(m_testDocsPath, "user.guides");
        Directory.CreateDirectory(docsDir);

        var docContent = "How to rent a bike: Step 1...";
        await File.WriteAllTextAsync(Path.Combine(docsDir, "test-guide.md"), docContent);

        var geminiResponse = new GeminiResponse
        {
            Candidates = new List<GeminiCandidate>
            {
                new GeminiCandidate
                {
                    Content = new GeminiContent
                    {
                        Parts = new List<GeminiPart> { new GeminiPart { Text = "The answer from Gemini" } }
                    }
                }
            }
        };

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(geminiResponse, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }))
            });

        var httpClient = new HttpClient(handlerMock.Object);
        m_httpClientFactoryMock.Setup(f => f.CreateClient("Gemini")).Returns(httpClient);

        if (string.IsNullOrEmpty(MotoConfig.GeminiApiKey))
        {
            Environment.SetEnvironmentVariable("MOTO_GeminiApiKey", "test-key");
        }

        var service = new DocumentationSearchService(m_httpClientFactoryMock.Object, m_loggerMock.Object, Path.Combine(m_testDocsPath, "user.guides"));

        // Act
        var result = await service.AskGeminiAsync("How do I rent?");

        // Assert
        Assert.Equal("The answer from Gemini", result);
        
        // Verify that the documentation context was indeed read
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Content != null && 
                req.Content.ReadAsStringAsync().Result.Contains(docContent)),
            ItExpr.IsAny<CancellationToken>()
        );

        // Cleanup
        Directory.Delete(m_testDocsPath, true);
    }
}