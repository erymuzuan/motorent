namespace MotoRent.Services.Gps;

/// <summary>
/// Provider-agnostic interface for GPS tracking providers.
/// Implementations: MockGpsProvider, Gps2GoProvider, FifotrackProvider, etc.
/// </summary>
public interface IGpsProvider
{
    /// <summary>
    /// Provider identifier (e.g., "GPS2GO", "Fifotrack", "Mock").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Whether this provider is currently configured and available.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Get all devices registered with this provider.
    /// </summary>
    Task<List<GpsDeviceInfo>> GetDevicesAsync(CancellationToken ct = default);

    /// <summary>
    /// Get the current position for a specific device.
    /// </summary>
    Task<GpsPositionDto?> GetCurrentPositionAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Get positions for multiple devices (batch operation).
    /// </summary>
    Task<Dictionary<string, GpsPositionDto?>> GetCurrentPositionsAsync(
        IEnumerable<string> deviceIds, CancellationToken ct = default);

    /// <summary>
    /// Get historical positions for a device within a time range.
    /// </summary>
    Task<List<GpsPositionDto>> GetHistoricalPositionsAsync(
        string deviceId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default);

    /// <summary>
    /// Get device status and health information.
    /// </summary>
    Task<DeviceStatusDto?> GetDeviceStatusAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Activate kill switch for a device (if supported).
    /// </summary>
    Task<KillSwitchResult> ActivateKillSwitchAsync(string deviceId, CancellationToken ct = default);

    /// <summary>
    /// Deactivate kill switch for a device.
    /// </summary>
    Task<KillSwitchResult> DeactivateKillSwitchAsync(string deviceId, CancellationToken ct = default);
}

/// <summary>
/// Device information from GPS provider.
/// </summary>
public class GpsDeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string? Imei { get; set; }
    public string? SimNumber { get; set; }
    public bool IsOnline { get; set; }
    public DateTimeOffset? LastContact { get; set; }
    public string? FirmwareVersion { get; set; }
}

/// <summary>
/// GPS position data from provider.
/// </summary>
public class GpsPositionDto
{
    public string DeviceId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public double? Heading { get; set; }
    public double? Accuracy { get; set; }
    public int? SatelliteCount { get; set; }
    public DateTimeOffset DeviceTimestamp { get; set; }
    public bool? IgnitionOn { get; set; }
    public int? BatteryPercent { get; set; }
    public string? RawJson { get; set; }

    /// <summary>
    /// Whether this is a valid GPS position.
    /// </summary>
    public bool IsValid => Math.Abs(Latitude) > 0.0001 || Math.Abs(Longitude) > 0.0001;
}

/// <summary>
/// Device status information.
/// </summary>
public class DeviceStatusDto
{
    public string DeviceId { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public int? BatteryPercent { get; set; }
    public bool? PowerConnected { get; set; }
    public bool? JammingDetected { get; set; }
    public DateTimeOffset? LastContact { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? SignalStrength { get; set; }
}

/// <summary>
/// Result of kill switch operation.
/// </summary>
public class KillSwitchResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTimeOffset? ActivatedAt { get; set; }

    public static KillSwitchResult Succeeded(DateTimeOffset? activatedAt = null) => new()
    {
        Success = true,
        ActivatedAt = activatedAt ?? DateTimeOffset.UtcNow
    };

    public static KillSwitchResult Failed(string error) => new()
    {
        Success = false,
        ErrorMessage = error
    };

    public static KillSwitchResult NotSupported() => new()
    {
        Success = false,
        ErrorMessage = "Kill switch not supported by this provider"
    };
}
