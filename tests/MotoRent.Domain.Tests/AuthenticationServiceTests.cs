using Moq;
using MotoRent.Domain.Core;
using MotoRent.Services.Core;
using Xunit;

namespace MotoRent.Domain.Tests;

public class AuthenticationServiceTests
{
    private readonly Mock<IDirectoryService> m_directoryServiceMock;
    private readonly AuthenticationService m_service;

    public AuthenticationServiceTests()
    {
        m_directoryServiceMock = new Mock<IDirectoryService>();
        m_service = new AuthenticationService(m_directoryServiceMock.Object);
    }

    [Fact]
    public async Task AuthenticateGoogleAsync_ShouldReturnExistingUser_WhenFoundByGoogleId()
    {
        // Arrange
        var existingUser = new User { UserName = "test@example.com", GoogleId = "google-123" };
        m_directoryServiceMock.Setup(s => s.GetUserByProviderIdAsync(User.GOOGLE, "google-123"))
            .ReturnsAsync(existingUser);

        // Act
        var result = await m_service.AuthenticateGoogleAsync("google-123", "test@example.com", "Test User");

        // Assert
        Assert.Same(existingUser, result);
    }

    [Fact]
    public async Task AuthenticateGoogleAsync_ShouldReturnUser_WhenFoundByEmailAndNoGoogleId()
    {
        // Arrange
        var existingUser = new User { UserName = "test@example.com", Email = "test@example.com" };
        m_directoryServiceMock.Setup(s => s.GetUserByProviderIdAsync(User.GOOGLE, "google-123"))
            .ReturnsAsync((User?)null);
        m_directoryServiceMock.Setup(s => s.GetUserAsync("test@example.com"))
            .ReturnsAsync(existingUser);

        // Act
        var result = await m_service.AuthenticateGoogleAsync("google-123", "test@example.com", "Test User");

        // Assert
        Assert.Same(existingUser, result);
        Assert.Equal("google-123", existingUser.GoogleId);
        m_directoryServiceMock.Verify(s => s.SaveUserProfileAsync(existingUser), Times.Once);
    }

    [Fact]
    public async Task AuthenticateLineAsync_ShouldReturnExistingUser_WhenFoundByLineId()
    {
        // Arrange
        var existingUser = new User { UserName = "line-123", LineId = "line-123" };
        m_directoryServiceMock.Setup(s => s.GetUserByProviderIdAsync(User.LINE, "line-123"))
            .ReturnsAsync(existingUser);

        // Act
        var result = await m_service.AuthenticateLineAsync("line-123", "Line User");

        // Assert
        Assert.Same(existingUser, result);
    }
}
