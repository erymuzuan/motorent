using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MotoRent.Domain.Core;

namespace MotoRent.Services.Gps;

/// <summary>
/// GPS2GO (BioWatch) provider implementation for Thailand GPS tracking.
/// API documentation: https://api.gps2go.co.th/docs
/// </summary>
public class Gps2GoProvider : IGpsProvider
{
    private readonly IHttpClientFactory m_httpClientFactory;
    private readonly ILogger<Gps2GoProvider> m_logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public Gps2GoProvider(IHttpClientFactory httpClientFactory, ILogger<Gps2GoProvider> logger)
    {
        m_httpClientFactory = httpClientFactory;
        m_logger = logger;
    }

    public string ProviderName => "GPS2GO";

    public bool IsConfigured => !string.IsNullOrEmpty(MotoConfig.Gps2GoApiKey);

    private string ApiKey => MotoConfig.Gps2GoApiKey ?? string.Empty;
    private string ApiUrl => MotoConfig.Gps2GoApiUrl;

    public async Task<List<GpsDeviceInfo>> GetDevicesAsync(CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            m_logger.LogWarning("GPS2GO API key not configured");
            return [];
        }

        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync($"{ApiUrl}/devices", ct);
            response.EnsureSuccessStatusCode();

            var gps2goDevices = await response.Content.ReadFromJsonAsync<Gps2GoDeviceListResponse>(s_jsonOptions, ct);

            return gps2goDevices?.Devices?.Select(d => new GpsDeviceInfo
            {
                DeviceId = d.DeviceId ?? string.Empty,
                Imei = d.Imei,
                SimNumber = d.SimNumber,
                IsOnline = d.Status == "online",
                LastContact = d.LastUpdate,
                FirmwareVersion = d.FirmwareVersion
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Error getting devices from GPS2GO");
            return [];
        }
    }

    public async Task<GpsPositionDto?> GetCurrentPositionAsync(string deviceId, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            m_logger.LogWarning("GPS2GO API key not configured");
            return null;
        }

        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync($"{ApiUrl}/devices/{deviceId}/position", ct);

            if (!response.IsSuccessStatusCode)
            {
                m_logger.LogWarning("GPS2GO returned {StatusCode} for device {DeviceId}",
                    response.StatusCode, deviceId);
                return null;
            }

            var gps2goPosition = await response.Content.ReadFromJsonAsync<Gps2GoPositionResponse>(s_jsonOptions, ct);

            if (gps2goPosition?.Position == null)
                return null;

            return new GpsPositionDto
            {
                DeviceId = deviceId,
                Latitude = gps2goPosition.Position.Latitude,
                Longitude = gps2goPosition.Position.Longitude,
                Altitude = gps2goPosition.Position.Altitude,
                Speed = gps2goPosition.Position.Speed,
                Heading = gps2goPosition.Position.Heading,
                Accuracy = gps2goPosition.Position.Accuracy,
                SatelliteCount = gps2goPosition.Position.Satellites,
                DeviceTimestamp = gps2goPosition.Position.Timestamp,
                IgnitionOn = gps2goPosition.Position.Ignition,
                BatteryPercent = gps2goPosition.Position.Battery,
                RawJson = JsonSerializer.Serialize(gps2goPosition, s_jsonOptions)
            };
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Error getting position for device {DeviceId} from GPS2GO", deviceId);
            return null;
        }
    }

    public async Task<Dictionary<string, GpsPositionDto?>> GetCurrentPositionsAsync(
        IEnumerable<string> deviceIds, CancellationToken ct = default)
    {
        // GPS2GO may support batch API - for now, query individually
        var result = new Dictionary<string, GpsPositionDto?>();

        foreach (var deviceId in deviceIds)
        {
            result[deviceId] = await GetCurrentPositionAsync(deviceId, ct);
        }

        return result;
    }

    public async Task<List<GpsPositionDto>> GetHistoricalPositionsAsync(
        string deviceId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            m_logger.LogWarning("GPS2GO API key not configured");
            return [];
        }

        try
        {
            var client = CreateHttpClient();
            var fromStr = from.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            var toStr = to.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");

            var response = await client.GetAsync(
                $"{ApiUrl}/devices/{deviceId}/history?from={fromStr}&to={toStr}", ct);
            response.EnsureSuccessStatusCode();

            var gps2goHistory = await response.Content.ReadFromJsonAsync<Gps2GoHistoryResponse>(s_jsonOptions, ct);

            return gps2goHistory?.Positions?.Select(p => new GpsPositionDto
            {
                DeviceId = deviceId,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                Altitude = p.Altitude,
                Speed = p.Speed,
                Heading = p.Heading,
                Accuracy = p.Accuracy,
                SatelliteCount = p.Satellites,
                DeviceTimestamp = p.Timestamp,
                IgnitionOn = p.Ignition,
                BatteryPercent = p.Battery
            }).ToList() ?? [];
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Error getting history for device {DeviceId} from GPS2GO", deviceId);
            return [];
        }
    }

    public async Task<DeviceStatusDto?> GetDeviceStatusAsync(string deviceId, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            m_logger.LogWarning("GPS2GO API key not configured");
            return null;
        }

        try
        {
            var client = CreateHttpClient();
            var response = await client.GetAsync($"{ApiUrl}/devices/{deviceId}/status", ct);

            if (!response.IsSuccessStatusCode)
                return null;

            var gps2goStatus = await response.Content.ReadFromJsonAsync<Gps2GoStatusResponse>(s_jsonOptions, ct);

            if (gps2goStatus == null)
                return null;

            return new DeviceStatusDto
            {
                DeviceId = deviceId,
                IsOnline = gps2goStatus.Status == "online",
                BatteryPercent = gps2goStatus.Battery,
                PowerConnected = gps2goStatus.PowerConnected,
                JammingDetected = gps2goStatus.JammingDetected,
                LastContact = gps2goStatus.LastContact,
                FirmwareVersion = gps2goStatus.FirmwareVersion,
                SignalStrength = gps2goStatus.SignalStrength
            };
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Error getting status for device {DeviceId} from GPS2GO", deviceId);
            return null;
        }
    }

    public async Task<KillSwitchResult> ActivateKillSwitchAsync(string deviceId, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return KillSwitchResult.Failed("GPS2GO API key not configured");
        }

        try
        {
            var client = CreateHttpClient();
            var response = await client.PostAsync(
                $"{ApiUrl}/devices/{deviceId}/kill-switch/activate",
                null,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return KillSwitchResult.Failed($"GPS2GO error: {error}");
            }

            m_logger.LogWarning("Kill switch ACTIVATED for device {DeviceId} via GPS2GO", deviceId);
            return KillSwitchResult.Succeeded();
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Error activating kill switch for device {DeviceId}", deviceId);
            return KillSwitchResult.Failed(ex.Message);
        }
    }

    public async Task<KillSwitchResult> DeactivateKillSwitchAsync(string deviceId, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return KillSwitchResult.Failed("GPS2GO API key not configured");
        }

        try
        {
            var client = CreateHttpClient();
            var response = await client.PostAsync(
                $"{ApiUrl}/devices/{deviceId}/kill-switch/deactivate",
                null,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                return KillSwitchResult.Failed($"GPS2GO error: {error}");
            }

            m_logger.LogInformation("Kill switch deactivated for device {DeviceId} via GPS2GO", deviceId);
            return KillSwitchResult.Succeeded();
        }
        catch (Exception ex)
        {
            m_logger.LogError(ex, "Error deactivating kill switch for device {DeviceId}", deviceId);
            return KillSwitchResult.Failed(ex.Message);
        }
    }

    private HttpClient CreateHttpClient()
    {
        var client = m_httpClientFactory.CreateClient("GPS2GO");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        return client;
    }

    #region GPS2GO Response Models

    private class Gps2GoDeviceListResponse
    {
        public List<Gps2GoDevice>? Devices { get; set; }
    }

    private class Gps2GoDevice
    {
        public string? DeviceId { get; set; }
        public string? Imei { get; set; }
        public string? SimNumber { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? LastUpdate { get; set; }
        public string? FirmwareVersion { get; set; }
    }

    private class Gps2GoPositionResponse
    {
        public Gps2GoPosition? Position { get; set; }
    }

    private class Gps2GoPosition
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Altitude { get; set; }
        public double? Speed { get; set; }
        public double? Heading { get; set; }
        public double? Accuracy { get; set; }
        public int? Satellites { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public bool? Ignition { get; set; }
        public int? Battery { get; set; }
    }

    private class Gps2GoHistoryResponse
    {
        public List<Gps2GoPosition>? Positions { get; set; }
    }

    private class Gps2GoStatusResponse
    {
        public string? Status { get; set; }
        public int? Battery { get; set; }
        public bool? PowerConnected { get; set; }
        public bool? JammingDetected { get; set; }
        public DateTimeOffset? LastContact { get; set; }
        public string? FirmwareVersion { get; set; }
        public string? SignalStrength { get; set; }
    }

    #endregion
}
