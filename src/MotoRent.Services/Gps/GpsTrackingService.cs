using Microsoft.Extensions.Logging;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;
using MotoRent.Domain.Extensions;

namespace MotoRent.Services.Gps;

/// <summary>
/// Orchestrates GPS tracking operations across providers.
/// Handles device management, position polling, and data storage.
/// </summary>
public partial class GpsTrackingService(
    RentalDataContext context,
    IEnumerable<IGpsProvider> providers,
    ILogger<GpsTrackingService> logger)
{
    private RentalDataContext Context { get; } = context;
    private IEnumerable<IGpsProvider> Providers { get; } = providers;
    private ILogger<GpsTrackingService> Logger { get; } = logger;

    /// <summary>
    /// Get configured GPS provider by name.
    /// </summary>
    public IGpsProvider? GetProvider(string providerName)
    {
        return this.Providers.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase) && p.IsConfigured);
    }

    /// <summary>
    /// Get all configured providers.
    /// </summary>
    public IEnumerable<IGpsProvider> GetConfiguredProviders()
    {
        return this.Providers.Where(p => p.IsConfigured);
    }

    /// <summary>
    /// Get the default provider (first configured, or Mock for development).
    /// </summary>
    public IGpsProvider GetDefaultProvider()
    {
        return this.GetConfiguredProviders().FirstOrDefault()
            ?? this.Providers.First(p => p.ProviderName == "Mock");
    }

    private static VehicleTrackingStatus DetermineTrackingStatus(
        GpsTrackingDevice device,
        GpsPosition? lastPosition)
    {
        if (device.JammingDetected)
            return VehicleTrackingStatus.JammingDetected;

        if (device.PowerDisconnected)
            return VehicleTrackingStatus.PowerDisconnected;

        if (device.BatteryPercent.HasValue && device.BatteryPercent < 20)
            return VehicleTrackingStatus.LowBattery;

        if (lastPosition is null)
            return VehicleTrackingStatus.Offline;

        // Consider offline if no position in last 30 minutes
        if (lastPosition.DeviceTimestamp < DateTimeOffset.UtcNow.AddMinutes(-30))
            return VehicleTrackingStatus.Offline;

        return VehicleTrackingStatus.Online;
    }
}

/// <summary>
/// Vehicle position with tracking status.
/// </summary>
public class VehiclePosition
{
    public Vehicle Vehicle { get; set; } = null!;
    public GpsPosition? LastPosition { get; set; }
    public GpsTrackingDevice? Device { get; set; }
    public bool IsOnline { get; set; }
    public VehicleTrackingStatus TrackingStatus { get; set; }
}

/// <summary>
/// Vehicle tracking status.
/// </summary>
public enum VehicleTrackingStatus
{
    Online,
    Offline,
    NoDevice,
    JammingDetected,
    PowerDisconnected,
    LowBattery
}
