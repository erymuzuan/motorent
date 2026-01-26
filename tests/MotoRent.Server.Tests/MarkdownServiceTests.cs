using MotoRent.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
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
    private class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager()
        {
            Initialize("https://localhost/", "https://localhost/");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            throw new NotImplementedException();
        }
    }

    private static MarkdownService CreateService(HttpClient httpClient, NavigationManager nav)
    {
        var logger = Mock.Of<ILogger<MarkdownService>>();
        return new MarkdownService(httpClient, nav, logger);
    }

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

        var nav = new TestNavigationManager();
        var service = CreateService(httpClient, nav);

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
        var nav = new TestNavigationManager();
        var service = CreateService(httpClient, nav);

        // Act
        var result = service.ToHtml("**bold**");

        // Assert
        Assert.Contains("<strong>bold</strong>", result);
    }
}
