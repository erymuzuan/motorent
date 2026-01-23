using MotoRent.Domain.Entities;
using MotoRent.Domain.Core;
using MotoRent.Services;
using Xunit;

namespace MotoRent.Domain.Tests;

public class TemplateDataResolverTests
{
    [Fact]
    public void Resolve_ShouldIncludeOrgAndStaffDetails()
    {
        // Arrange
        var resolver = new TemplateDataResolver();
        var org = new Organization { Name = "MotoRent Phuket", Email = "phuket@motorent.com" };
        var staff = new User { FullName = "John Doe", UserName = "john" };
        var booking = new Booking { CustomerName = "Alice Smith" };

        // Act
        var result = resolver.Resolve(booking, org, staff);

        // Assert
        Assert.Equal("MotoRent Phuket", result["Org.Name"]);
        Assert.Equal("phuket@motorent.com", result["Org.Email"]);
        Assert.Equal("John Doe", result["Staff.Name"]);
        Assert.Equal("Alice Smith", result["Booking.CustomerName"]);
    }

    [Fact]
    public void Resolve_Booking_ShouldIncludeCalculatedFields()
    {
        // Arrange
        var resolver = new TemplateDataResolver();
        var org = new Organization();
        var start = DateTimeOffset.UtcNow;
        var booking = new Booking
        {
            StartDate = start,
            EndDate = start.AddDays(3),
            TotalAmount = 3000,
            AmountPaid = 1000
        };

        // Act
        var result = resolver.Resolve(booking, org);

        // Assert
        Assert.Equal(3000m, result["Booking.TotalAmount"]);
        Assert.Equal(1000m, result["Booking.AmountPaid"]);
        Assert.Equal(2000m, result["Booking.BalanceDue"]);
        Assert.Equal(3, result["Booking.Days"]);
    }
}
