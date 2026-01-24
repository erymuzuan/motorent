using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Server.Controllers;
using Xunit;

namespace MotoRent.Server.Tests;

public class OnboardingControllerTests
{
    private readonly Mock<IOnboardingService> m_onboardingServiceMock;
    private readonly Mock<CoreDataContext> m_coreDataContextMock;
    private readonly Mock<ILogger<OnboardingController>> m_loggerMock;
    private readonly OnboardingController m_controller;

    public OnboardingControllerTests()
    {
        m_onboardingServiceMock = new Mock<IOnboardingService>();
        m_coreDataContextMock = new Mock<CoreDataContext>(new Mock<IServiceProvider>().Object);
        m_loggerMock = new Mock<ILogger<OnboardingController>>();
        
        m_controller = new OnboardingController(
            m_onboardingServiceMock.Object,
            m_coreDataContextMock.Object,
            m_loggerMock.Object);
    }

    [Fact]
    public async Task Submit_ShouldReturnOk_WhenOnboardingSucceeds()
    {
        // Arrange
        var request = new OnboardingRequest { ShopName = "Test Shop", Email = "test@example.com" };
        var org = new Organization { AccountNo = "testshop", Name = "Test Shop" };
        
        m_onboardingServiceMock.Setup(s => s.OnboardAsync(request))
            .ReturnsAsync(org);

        // Act
        var result = await m_controller.Submit(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<OnboardingResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal("testshop", response.AccountNo);
    }

    [Fact]
    public async Task Submit_ShouldReturn500_WhenOnboardingFails()
    {
        // Arrange
        var request = new OnboardingRequest { ShopName = "Test Shop" };
        m_onboardingServiceMock.Setup(s => s.OnboardAsync(request))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await m_controller.Submit(request);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
}
