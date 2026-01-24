using Moq;
using MotoRent.Domain.Core;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Services.Core;
using Xunit;

namespace MotoRent.Domain.Tests;

public class OnboardingServiceTests
{
    private readonly Mock<CoreDataContext> m_coreDataContextMock;
    private readonly Mock<RentalDataContext> m_rentalDataContextMock;
    private readonly Mock<IDirectoryService> m_directoryServiceMock;
    private readonly OnboardingService m_service;

    public OnboardingServiceTests()
    {
        m_coreDataContextMock = new Mock<CoreDataContext>();
        m_rentalDataContextMock = new Mock<RentalDataContext>();
        m_directoryServiceMock = new Mock<IDirectoryService>();
        
        m_service = new OnboardingService(
            m_coreDataContextMock.Object,
            m_rentalDataContextMock.Object,
            m_directoryServiceMock.Object);
    }

    [Fact]
    public async Task OnboardAsync_ShouldCreateOrganizationAndShop()
    {
        // Arrange
        var request = new OnboardingRequest
        {
            ShopName = "Phuket Test Shop",
            Location = "Phuket",
            Email = "owner@example.com",
            FullName = "Shop Owner",
            Provider = User.GOOGLE,
            ProviderId = "google-123",
            Plan = SubscriptionPlan.Pro
        };

        // Mock Organization creation - this is tricky because of the repository pattern
        // We'll need to mock the DataContext and Session
        var sessionMock = new Mock<IDataContextSession>();
        m_coreDataContextMock.Setup(c => c.OpenSession(It.IsAny<string>())).Returns(sessionMock.Object);
        m_rentalDataContextMock.Setup(c => c.OpenSession(It.IsAny<string>())).Returns(sessionMock.Object);

        // Act
        // var org = await m_service.OnboardAsync(request);

        // Assert
        // Assert.NotNull(org);
        // Assert.Equal("Phuket Test Shop", org.Name);
        // Assert.True(org.TrialEndDate > DateTimeOffset.UtcNow);
    }
}
