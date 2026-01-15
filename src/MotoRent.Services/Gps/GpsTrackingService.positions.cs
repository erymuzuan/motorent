using Microsoft.Extensions.Logging;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;

namespace MotoRent.Services.Gps;

/// <summary>
/// Position tracking operations.
/// </summary>
public partial class GpsTrackingService
{
    /// <summary>
    /// Get current position for a vehicle.
    /// </summary>
    public async Task<GpsPosition?> GetCurrentPositionAsync(int vehicleId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<GpsPosition>()
                .Where(p => p.VehicleId == vehicleId)
                .OrderByDescending(p => p.DeviceTimestamp),
            1, 1);

        return result.ItemCollection.FirstOrDefault();
    }

    /// <summary>
    /// Get current positions for all vehicles in a shop.
    /// </summary>
    public async Task<List<VehiclePosition>> GetFleetPositionsAsync(int shopId)
    {
        // Get vehicles at this shop
        var vehiclesResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<Vehicle>()
                .Where(v => v.HomeShopId == shopId || v.CurrentShopId == shopId),
            1, 500);

        var vehicles = vehiclesResult.ItemCollection;
        var vehicleIds = vehicles.Select(v => v.VehicleId).ToList();

        // Get devices for these vehicles using IsInList for WHERE IN
        var devicesResult = await this.Context.LoadAsync(
            this.Context.CreateQuery<GpsTrackingDevice>()
                .Where(d => d.IsActive && vehicleIds.IsInList(d.VehicleId)),
            1, 500);

        var devices = devicesResult.ItemCollection.ToDictionary(d => d.VehicleId);

        // Get latest positions for vehicles with devices
        var positions = new List<VehiclePosition>();

        foreach (var vehicle in vehicles)
        {
            var position = new VehiclePosition
            {
                Vehicle = vehicle,
                Device = devices.GetValueOrDefault(vehicle.VehicleId),
                TrackingStatus = VehicleTrackingStatus.NoDevice
            };

            if (position.Device is not null)
            {
                var lastPosition = await this.GetCurrentPositionAsync(vehicle.VehicleId);
                position.LastPosition = lastPosition;
                position.TrackingStatus = DetermineTrackingStatus(position.Device, lastPosition);
                position.IsOnline = position.TrackingStatus == VehicleTrackingStatus.Online;
            }

            positions.Add(position);
        }

        return positions;
    }

    /// <summary>
    /// Get position history for a vehicle.
    /// </summary>
    public async Task<List<GpsPosition>> GetPositionHistoryAsync(
        int vehicleId,
        DateTimeOffset from,
        DateTimeOffset to)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<GpsPosition>()
                .Where(p => p.VehicleId == vehicleId &&
                           p.DeviceTimestamp >= from &&
                           p.DeviceTimestamp <= to)
                .OrderBy(p => p.DeviceTimestamp),
            1, 10000);

        return result.ItemCollection;
    }

    /// <summary>
    /// Poll a device and store the position.
    /// </summary>
    public async Task<GpsPosition?> PollDeviceAsync(GpsTrackingDevice device, string username)
    {
        var provider = this.GetProvider(device.Provider);
        if (provider is null)
        {
            this.Logger.LogWarning("Provider {Provider} not found for device {DeviceId}",
                device.Provider, device.DeviceId);
            return null;
        }

        try
        {
            var positionDto = await provider.GetCurrentPositionAsync(device.DeviceId);
            if (positionDto is null || !positionDto.IsValid)
            {
                return null;
            }

            // Get vehicle for license plate
            var vehicle = await this.Context.LoadOneAsync<Vehicle>(v => v.VehicleId == device.VehicleId);

            var position = new GpsPosition
            {
                GpsTrackingDeviceId = device.GpsTrackingDeviceId,
                VehicleId = device.VehicleId,
                Latitude = positionDto.Latitude,
                Longitude = positionDto.Longitude,
                Altitude = positionDto.Altitude,
                Speed = positionDto.Speed,
                Heading = positionDto.Heading,
                Accuracy = positionDto.Accuracy,
                SatelliteCount = positionDto.SatelliteCount,
                DeviceTimestamp = positionDto.DeviceTimestamp,
                ReceivedTimestamp = DateTimeOffset.UtcNow,
                IgnitionOn = positionDto.IgnitionOn,
                RawData = positionDto.RawJson,
                VehicleLicensePlate = vehicle?.LicensePlate
            };

            using var session = this.Context.OpenSession(username);
            session.Attach(position);

            var result = await session.SubmitChanges("StoreGpsPosition");
            if (!result.Success)
            {
                this.Logger.LogError("Failed to store position: {Error}", result.Message);
                return null;
            }

            // Update device last contact
            device.LastContactTimestamp = DateTimeOffset.UtcNow;
            device.BatteryPercent = positionDto.BatteryPercent;

            using var deviceSession = this.Context.OpenSession(username);
            deviceSession.Attach(device);
            await deviceSession.SubmitChanges("UpdateDeviceContact");

            return position;
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error polling device {DeviceId}", device.DeviceId);
            return null;
        }
    }

    /// <summary>
    /// Poll all active devices and store positions.
    /// Returns count of successful polls.
    /// </summary>
    public async Task<int> PollAllDevicesAsync(string username, CancellationToken ct = default)
    {
        var devices = await this.GetActiveDevicesAsync();
        var successCount = 0;

        foreach (var device in devices)
        {
            if (ct.IsCancellationRequested)
                break;

            var position = await this.PollDeviceAsync(device, username);
            if (position is not null)
            {
                successCount++;
            }
        }

        this.Logger.LogInformation("Polled {SuccessCount}/{TotalCount} GPS devices",
            successCount, devices.Count);

        return successCount;
    }
}
