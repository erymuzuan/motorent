using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a GPS tracking device installed in a vehicle.
/// Supports multiple providers (GPS2GO, Fifotrack, Heliot, etc.).
/// </summary>
public class GpsTrackingDevice : Entity
{
    public int GpsTrackingDeviceId { get; set; }

    /// <summary>
    /// The vehicle this device is installed in.
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// GPS provider name (GPS2GO, Fifotrack, Heliot, AIS, etc.).
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Device identifier from the GPS provider.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// SIM card number for 4G connectivity.
    /// </summary>
    public string? SimNumber { get; set; }

    /// <summary>
    /// IMEI number of the device.
    /// </summary>
    public string? Imei { get; set; }

    /// <summary>
    /// Device installation date.
    /// </summary>
    public DateTimeOffset InstalledDate { get; set; }

    /// <summary>
    /// Last time data was received from this device.
    /// </summary>
    public DateTimeOffset? LastContactTimestamp { get; set; }

    /// <summary>
    /// Device battery percentage (for backup battery monitoring).
    /// </summary>
    public int? BatteryPercent { get; set; }

    /// <summary>
    /// Whether the device is currently active and reporting.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether GPS jamming has been detected.
    /// </summary>
    public bool JammingDetected { get; set; }

    /// <summary>
    /// Whether the main power is disconnected (running on backup battery).
    /// </summary>
    public bool PowerDisconnected { get; set; }

    /// <summary>
    /// Kill switch feature availability.
    /// </summary>
    public bool SupportsKillSwitch { get; set; }

    /// <summary>
    /// UI placeholder - kill switch activation status.
    /// Note: Does not actually communicate with hardware.
    /// </summary>
    public bool KillSwitchActivated { get; set; }

    /// <summary>
    /// When the kill switch was activated (for UI display).
    /// </summary>
    public DateTimeOffset? KillSwitchActivatedAt { get; set; }

    /// <summary>
    /// Reason for kill switch activation (for UI display).
    /// </summary>
    public string? KillSwitchReason { get; set; }

    /// <summary>
    /// Provider-specific configuration JSON.
    /// </summary>
    public string? ProviderConfig { get; set; }

    /// <summary>
    /// Notes about installation or device.
    /// </summary>
    public string? Notes { get; set; }

    // Denormalized fields for display

    /// <summary>
    /// Vehicle license plate for display.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleLicensePlate { get; set; }

    /// <summary>
    /// Vehicle display name for display.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleDisplayName { get; set; }

    public override int GetId() => this.GpsTrackingDeviceId;
    public override void SetId(int value) => this.GpsTrackingDeviceId = value;

    /// <summary>
    /// Gets the current status of the device.
    /// </summary>
    [JsonIgnore]
    public DeviceStatus CurrentStatus
    {
        get
        {
            if (!IsActive)
                return DeviceStatus.Inactive;
            if (JammingDetected)
                return DeviceStatus.JammingDetected;
            if (PowerDisconnected)
                return DeviceStatus.PowerDisconnected;
            if (BatteryPercent.HasValue && BatteryPercent < 20)
                return DeviceStatus.LowBattery;
            if (LastContactTimestamp.HasValue &&
                LastContactTimestamp.Value < DateTimeOffset.UtcNow.AddMinutes(-30))
                return DeviceStatus.NoSignal;
            return DeviceStatus.Online;
        }
    }
}

/// <summary>
/// Device operational status.
/// </summary>
public enum DeviceStatus
{
    /// <summary>Device is online and reporting normally.</summary>
    Online,
    /// <summary>Device is offline or not reporting.</summary>
    Offline,
    /// <summary>Device is administratively disabled.</summary>
    Inactive,
    /// <summary>No signal received recently.</summary>
    NoSignal,
    /// <summary>GPS jamming detected.</summary>
    JammingDetected,
    /// <summary>Main power disconnected, running on backup.</summary>
    PowerDisconnected,
    /// <summary>Backup battery is low.</summary>
    LowBattery
}
