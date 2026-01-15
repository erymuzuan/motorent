using Microsoft.Extensions.Logging;
using MotoRent.Domain.DataContext;

namespace MotoRent.Services.Gps;

/// <summary>
/// Kill switch placeholder operations (UI only, no hardware integration).
/// </summary>
public partial class GpsTrackingService
{
    /// <summary>
    /// UI placeholder for kill switch activation.
    /// Updates the device record but does not communicate with hardware.
    /// </summary>
    public async Task<SubmitOperation> ActivateKillSwitchPlaceholderAsync(
        int vehicleId,
        string username,
        string reason)
    {
        var device = await this.GetDeviceForVehicleAsync(vehicleId);
        if (device is null)
        {
            return SubmitOperation.CreateFailure("No GPS device registered for this vehicle");
        }

        device.KillSwitchActivated = true;
        device.KillSwitchActivatedAt = DateTimeOffset.UtcNow;
        device.KillSwitchReason = reason;

        using var session = this.Context.OpenSession(username);
        session.Attach(device);

        var result = await session.SubmitChanges("ActivateKillSwitch");

        if (result.Success)
        {
            this.Logger.LogWarning(
                "Kill switch PLACEHOLDER activated for vehicle {VehicleId} by {Username}. Reason: {Reason}",
                vehicleId, username, reason);
        }

        return result;
    }

    /// <summary>
    /// UI placeholder for kill switch deactivation.
    /// </summary>
    public async Task<SubmitOperation> DeactivateKillSwitchPlaceholderAsync(
        int vehicleId,
        string username)
    {
        var device = await this.GetDeviceForVehicleAsync(vehicleId);
        if (device is null)
        {
            return SubmitOperation.CreateFailure("No GPS device registered for this vehicle");
        }

        device.KillSwitchActivated = false;
        device.KillSwitchActivatedAt = null;
        device.KillSwitchReason = null;

        using var session = this.Context.OpenSession(username);
        session.Attach(device);

        var result = await session.SubmitChanges("DeactivateKillSwitch");

        if (result.Success)
        {
            this.Logger.LogInformation(
                "Kill switch placeholder deactivated for vehicle {VehicleId} by {Username}",
                vehicleId, username);
        }

        return result;
    }
}
