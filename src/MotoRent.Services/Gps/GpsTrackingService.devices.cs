using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;

namespace MotoRent.Services.Gps;

/// <summary>
/// Device management operations.
/// </summary>
public partial class GpsTrackingService
{
    /// <summary>
    /// Register a new GPS device for a vehicle.
    /// </summary>
    public async Task<SubmitOperation> RegisterDeviceAsync(
        int vehicleId,
        string provider,
        string deviceId,
        string? imei,
        string? simNumber,
        string username)
    {
        // Check if vehicle already has a device
        var existingResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<GpsTrackingDevice>()
                .Where(d => d.VehicleId == vehicleId),
            1, 1);

        if (existingResult.ItemCollection.Count > 0)
        {
            return SubmitOperation.CreateFailure("Vehicle already has a GPS device registered");
        }

        // Get vehicle for denormalized fields
        var vehicle = await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == vehicleId);

        if (vehicle is null)
        {
            return SubmitOperation.CreateFailure("Vehicle not found");
        }

        var device = new GpsTrackingDevice
        {
            VehicleId = vehicleId,
            Provider = provider,
            DeviceId = deviceId,
            Imei = imei,
            SimNumber = simNumber,
            InstalledDate = DateTimeOffset.UtcNow,
            IsActive = true,
            VehicleLicensePlate = vehicle.LicensePlate,
            VehicleDisplayName = vehicle.DisplayName
        };

        using var session = this.Context.OpenSession(username);
        session.Attach(device);

        return await session.SubmitChanges("RegisterGpsDevice");
    }

    /// <summary>
    /// Get GPS device for a vehicle.
    /// </summary>
    public async Task<GpsTrackingDevice?> GetDeviceForVehicleAsync(int vehicleId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<GpsTrackingDevice>()
                .Where(d => d.VehicleId == vehicleId && d.IsActive),
            1, 1);

        return result.ItemCollection.FirstOrDefault();
    }

    /// <summary>
    /// Get all active GPS devices.
    /// </summary>
    public async Task<List<GpsTrackingDevice>> GetActiveDevicesAsync()
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<GpsTrackingDevice>().Where(d => d.IsActive),
            1, 1000);

        return result.ItemCollection;
    }

    /// <summary>
    /// Update device status from provider.
    /// </summary>
    public async Task<SubmitOperation> UpdateDeviceStatusAsync(
        int deviceId,
        bool? jammingDetected,
        bool? powerDisconnected,
        int? batteryPercent,
        string username)
    {
        var device = await this.Context.LoadOneAsync<GpsTrackingDevice>(d => d.GpsTrackingDeviceId == deviceId);
        if (device is null)
        {
            return SubmitOperation.CreateFailure("Device not found");
        }

        device.JammingDetected = jammingDetected ?? device.JammingDetected;
        device.PowerDisconnected = powerDisconnected ?? device.PowerDisconnected;
        device.BatteryPercent = batteryPercent ?? device.BatteryPercent;
        device.LastContactTimestamp = DateTimeOffset.UtcNow;

        using var session = this.Context.OpenSession(username);
        session.Attach(device);

        return await session.SubmitChanges("UpdateDeviceStatus");
    }

    /// <summary>
    /// Deactivate a GPS device.
    /// </summary>
    public async Task<SubmitOperation> DeactivateDeviceAsync(int deviceId, string username)
    {
        var device = await this.Context.LoadOneAsync<GpsTrackingDevice>(d => d.GpsTrackingDeviceId == deviceId);
        if (device is null)
        {
            return SubmitOperation.CreateFailure("Device not found");
        }

        device.IsActive = false;

        using var session = this.Context.OpenSession(username);
        session.Attach(device);

        return await session.SubmitChanges("DeactivateGpsDevice");
    }
}
