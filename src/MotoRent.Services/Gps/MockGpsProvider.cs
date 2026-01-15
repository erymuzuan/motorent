using Microsoft.Extensions.Logging;

namespace MotoRent.Services.Gps;

/// <summary>
/// Mock GPS provider for testing and development.
/// Generates simulated GPS positions around Thailand tourist areas.
/// </summary>
public class MockGpsProvider : IGpsProvider
{
    private readonly ILogger<MockGpsProvider> m_logger;
    private readonly Random m_random = new();

    // Simulated device data
    private readonly Dictionary<string, MockDeviceState> m_devices = new();

    // Thailand tourist area centers
    private static readonly (double Lat, double Lng, string Name)[] s_touristAreas =
    [
        (7.8804, 98.3923, "Phuket"),
        (7.8913, 98.2987, "Patong Beach"),
        (7.8205, 98.4039, "Kata Beach"),
        (8.0863, 98.9063, "Krabi"),
        (9.5120, 100.0136, "Koh Samui"),
        (8.4186, 99.9587, "Surat Thani")
    ];

    public MockGpsProvider(ILogger<MockGpsProvider> logger)
    {
        m_logger = logger;
    }

    public string ProviderName => "Mock";

    public bool IsConfigured => true;

    /// <summary>
    /// Register a mock device for simulation.
    /// </summary>
    public void RegisterDevice(string deviceId, double? startLat = null, double? startLng = null)
    {
        var area = s_touristAreas[m_random.Next(s_touristAreas.Length)];
        m_devices[deviceId] = new MockDeviceState
        {
            DeviceId = deviceId,
            Latitude = startLat ?? area.Lat + (m_random.NextDouble() - 0.5) * 0.1,
            Longitude = startLng ?? area.Lng + (m_random.NextDouble() - 0.5) * 0.1,
            IsOnline = true,
            BatteryPercent = m_random.Next(60, 100),
            IgnitionOn = m_random.NextDouble() > 0.3,
            LastUpdate = DateTimeOffset.UtcNow
        };
    }

    public Task<List<GpsDeviceInfo>> GetDevicesAsync(CancellationToken ct = default)
    {
        var devices = m_devices.Values.Select(d => new GpsDeviceInfo
        {
            DeviceId = d.DeviceId,
            Imei = $"MOCK{d.DeviceId.PadLeft(15, '0')}",
            IsOnline = d.IsOnline,
            LastContact = d.LastUpdate
        }).ToList();

        return Task.FromResult(devices);
    }

    public Task<GpsPositionDto?> GetCurrentPositionAsync(string deviceId, CancellationToken ct = default)
    {
        if (!m_devices.TryGetValue(deviceId, out var device))
        {
            // Auto-register unknown devices
            RegisterDevice(deviceId);
            device = m_devices[deviceId];
        }

        // Simulate movement
        SimulateMovement(device);

        var position = new GpsPositionDto
        {
            DeviceId = deviceId,
            Latitude = device.Latitude,
            Longitude = device.Longitude,
            Speed = device.IgnitionOn ? m_random.Next(0, 60) : 0,
            Heading = m_random.Next(0, 360),
            Accuracy = m_random.Next(3, 15),
            SatelliteCount = m_random.Next(6, 12),
            DeviceTimestamp = DateTimeOffset.UtcNow,
            IgnitionOn = device.IgnitionOn,
            BatteryPercent = device.BatteryPercent
        };

        return Task.FromResult<GpsPositionDto?>(position);
    }

    public async Task<Dictionary<string, GpsPositionDto?>> GetCurrentPositionsAsync(
        IEnumerable<string> deviceIds, CancellationToken ct = default)
    {
        var result = new Dictionary<string, GpsPositionDto?>();

        foreach (var deviceId in deviceIds)
        {
            result[deviceId] = await GetCurrentPositionAsync(deviceId, ct);
        }

        return result;
    }

    public Task<List<GpsPositionDto>> GetHistoricalPositionsAsync(
        string deviceId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        if (!m_devices.TryGetValue(deviceId, out var device))
        {
            RegisterDevice(deviceId);
            device = m_devices[deviceId];
        }

        var positions = new List<GpsPositionDto>();
        var interval = TimeSpan.FromMinutes(10);
        var current = from;

        var lat = device.Latitude;
        var lng = device.Longitude;

        while (current <= to)
        {
            // Simulate historical movement
            lat += (m_random.NextDouble() - 0.5) * 0.005;
            lng += (m_random.NextDouble() - 0.5) * 0.005;

            positions.Add(new GpsPositionDto
            {
                DeviceId = deviceId,
                Latitude = lat,
                Longitude = lng,
                Speed = m_random.Next(0, 50),
                Heading = m_random.Next(0, 360),
                Accuracy = m_random.Next(3, 15),
                SatelliteCount = m_random.Next(6, 12),
                DeviceTimestamp = current,
                IgnitionOn = m_random.NextDouble() > 0.3,
                BatteryPercent = m_random.Next(50, 100)
            });

            current = current.Add(interval);
        }

        return Task.FromResult(positions);
    }

    public Task<DeviceStatusDto?> GetDeviceStatusAsync(string deviceId, CancellationToken ct = default)
    {
        if (!m_devices.TryGetValue(deviceId, out var device))
        {
            return Task.FromResult<DeviceStatusDto?>(null);
        }

        var status = new DeviceStatusDto
        {
            DeviceId = deviceId,
            IsOnline = device.IsOnline,
            BatteryPercent = device.BatteryPercent,
            PowerConnected = device.BatteryPercent > 80,
            JammingDetected = false,
            LastContact = device.LastUpdate,
            FirmwareVersion = "MOCK-1.0.0",
            SignalStrength = "Good"
        };

        return Task.FromResult<DeviceStatusDto?>(status);
    }

    public Task<KillSwitchResult> ActivateKillSwitchAsync(string deviceId, CancellationToken ct = default)
    {
        m_logger.LogWarning("Mock kill switch activated for device {DeviceId}", deviceId);

        if (m_devices.TryGetValue(deviceId, out var device))
        {
            device.KillSwitchActive = true;
            device.IgnitionOn = false;
        }

        return Task.FromResult(KillSwitchResult.Succeeded());
    }

    public Task<KillSwitchResult> DeactivateKillSwitchAsync(string deviceId, CancellationToken ct = default)
    {
        m_logger.LogInformation("Mock kill switch deactivated for device {DeviceId}", deviceId);

        if (m_devices.TryGetValue(deviceId, out var device))
        {
            device.KillSwitchActive = false;
        }

        return Task.FromResult(KillSwitchResult.Succeeded());
    }

    private void SimulateMovement(MockDeviceState device)
    {
        if (!device.IgnitionOn || device.KillSwitchActive)
            return;

        // Random movement within ~100m
        device.Latitude += (m_random.NextDouble() - 0.5) * 0.002;
        device.Longitude += (m_random.NextDouble() - 0.5) * 0.002;
        device.LastUpdate = DateTimeOffset.UtcNow;

        // Occasionally toggle ignition
        if (m_random.NextDouble() < 0.05)
        {
            device.IgnitionOn = !device.IgnitionOn;
        }

        // Battery drain simulation
        if (device.BatteryPercent > 20 && m_random.NextDouble() < 0.1)
        {
            device.BatteryPercent--;
        }
    }

    private class MockDeviceState
    {
        public string DeviceId { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public bool IsOnline { get; set; }
        public bool IgnitionOn { get; set; }
        public int BatteryPercent { get; set; }
        public bool KillSwitchActive { get; set; }
        public DateTimeOffset LastUpdate { get; set; }
    }
}
