using MotoRent.Domain.Entities;
using Xunit;

namespace MotoRent.Domain.Tests;

public class MaintenanceAlertTests
{
    [Fact]
    public void MaintenanceAlert_ShouldSetProperties()
    {
        // Arrange
        var alert = new MaintenanceAlert
        {
            MaintenanceAlertId = 1,
            VehicleId = 10,
            ServiceTypeId = 5,
            Status = MaintenanceStatus.Overdue,
            TriggerMileage = 1000,
            VehicleName = "Honda Click",
            LicensePlate = "ABC-123"
        };

        // Assert
        Assert.Equal(1, alert.MaintenanceAlertId);
        Assert.Equal(10, alert.VehicleId);
        Assert.Equal(5, alert.ServiceTypeId);
        Assert.Equal(MaintenanceStatus.Overdue, alert.Status);
        Assert.Equal(1000, alert.TriggerMileage);
        Assert.Equal("Honda Click", alert.VehicleName);
        Assert.Equal("ABC-123", alert.LicensePlate);
        Assert.False(alert.IsRead);
    }

    [Fact]
    public void GetId_ShouldReturnMaintenanceAlertId()
    {
        // Arrange
        var alert = new MaintenanceAlert { MaintenanceAlertId = 42 };

        // Assert
        Assert.Equal(42, alert.GetId());
    }

    [Fact]
    public void SetId_ShouldSetMaintenanceAlertId()
    {
        // Arrange
        var alert = new MaintenanceAlert();

        // Act
        alert.SetId(99);

        // Assert
        Assert.Equal(99, alert.MaintenanceAlertId);
        Assert.Equal(99, alert.GetId());
    }
}
