using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a GPS position record from a tracking device.
/// 90-day retention policy applies.
/// </summary>
public class GpsPosition : Entity
{
    public int GpsPositionId { get; set; }

    /// <summary>
    /// The device that reported this position.
    /// </summary>
    public int GpsTrackingDeviceId { get; set; }

    /// <summary>
    /// The vehicle (denormalized for query efficiency).
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Latitude in decimal degrees.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Longitude in decimal degrees.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Altitude in meters (optional).
    /// </summary>
    public double? Altitude { get; set; }

    /// <summary>
    /// Speed in km/h at time of reading.
    /// </summary>
    public double? Speed { get; set; }

    /// <summary>
    /// Heading/course in degrees (0-360).
    /// </summary>
    public double? Heading { get; set; }

    /// <summary>
    /// GPS accuracy in meters.
    /// </summary>
    public double? Accuracy { get; set; }

    /// <summary>
    /// Number of satellites used for fix.
    /// </summary>
    public int? SatelliteCount { get; set; }

    /// <summary>
    /// When the device recorded this position.
    /// </summary>
    public DateTimeOffset DeviceTimestamp { get; set; }

    /// <summary>
    /// When we received this position from the provider.
    /// </summary>
    public DateTimeOffset ReceivedTimestamp { get; set; }

    /// <summary>
    /// Whether the vehicle ignition is on.
    /// </summary>
    public bool? IgnitionOn { get; set; }

    /// <summary>
    /// Raw provider data JSON (for debugging).
    /// </summary>
    public string? RawData { get; set; }

    /// <summary>
    /// Denormalized license plate for queries.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleLicensePlate { get; set; }

    public override int GetId() => this.GpsPositionId;
    public override void SetId(int value) => this.GpsPositionId = value;

    /// <summary>
    /// Gets a LatLng representation of this position.
    /// </summary>
    [JsonIgnore]
    public Spatial.LatLng Location => new(Latitude, Longitude)
    {
        Elevation = Altitude
    };

    /// <summary>
    /// Gets the Google Maps URL for this position.
    /// </summary>
    [JsonIgnore]
    public string GoogleMapsUrl => $"https://www.google.com/maps?q={Latitude},{Longitude}";

    /// <summary>
    /// Whether this is a valid GPS position.
    /// </summary>
    [JsonIgnore]
    public bool IsValid => Math.Abs(Latitude) > 0.0001 || Math.Abs(Longitude) > 0.0001;
}
