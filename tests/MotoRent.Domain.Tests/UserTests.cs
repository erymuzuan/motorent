using MotoRent.Domain.Core;
using Xunit;

namespace MotoRent.Domain.Tests;

public class UserTests
{
    [Fact]
    public void User_ShouldHaveOAuthProviderFields()
    {
        // Arrange
        var user = new User();

        // Act
        user.GoogleId = "google-123";
        user.LineId = "line-456";

        // Assert
        Assert.Equal("google-123", user.GoogleId);
        Assert.Equal("line-456", user.LineId);
    }
}
