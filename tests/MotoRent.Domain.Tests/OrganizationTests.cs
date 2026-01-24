using MotoRent.Domain.Core;
using Xunit;

namespace MotoRent.Domain.Tests;

public class OrganizationTests
{
    [Fact]
    public void Organization_ShouldHaveSaaSSubscriptionFields()
    {
        // Arrange
        var org = new Organization();
        var trialEndDate = DateTimeOffset.UtcNow.AddDays(30);

        // Act & Assert
        org.SubscriptionPlan = SubscriptionPlan.Pro;
        org.TrialEndDate = trialEndDate;
        org.PreferredLanguage = "th";

        Assert.Equal(SubscriptionPlan.Pro, org.SubscriptionPlan);
        Assert.Equal(trialEndDate, org.TrialEndDate);
        Assert.Equal("th", org.PreferredLanguage);
    }

    [Fact]
    public void IsTrialActive_ShouldReturnTrue_WhenEndDateIsInFuture()
    {
        // Arrange
        var org = new Organization();
        org.TrialEndDate = DateTimeOffset.UtcNow.AddDays(1);

        // Act & Assert
        Assert.True(org.IsTrialActive);
    }

    [Fact]
    public void IsTrialActive_ShouldReturnFalse_WhenEndDateIsInPast()
    {
        // Arrange
        var org = new Organization();
        org.TrialEndDate = DateTimeOffset.UtcNow.AddDays(-1);

        // Act & Assert
        Assert.False(org.IsTrialActive);
    }
}
