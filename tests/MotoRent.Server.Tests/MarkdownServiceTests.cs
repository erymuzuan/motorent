using MotoRent.Client.Services;
using System.Net.Http;
using Moq;
using Moq.Protected;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MotoRent.Server.Tests;

public class MarkdownServiceTests
{
    [Fact]
    public async Task RenderMarkdownAsync_ShouldReturnHtml()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(new HttpResponseMessage()
           {
               StatusCode = HttpStatusCode.OK,
               Content = new StringContent("# Hello World"),
           })
           .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new System.Uri("https://localhost/")
        };
        var service = new MarkdownService(httpClient);

        // Act
        var result = await service.RenderMarkdownAsync("test.md");

        // Assert
        Assert.Contains("Hello World", result);
        Assert.Contains("h1", result);
    }

    [Fact]
    public void ToHtml_ShouldConvertMarkdownToHtml()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new MarkdownService(httpClient);

        // Act
        var result = service.ToHtml("**bold**");

        // Assert
        Assert.Contains("<strong>bold</strong>", result);
    }
}
