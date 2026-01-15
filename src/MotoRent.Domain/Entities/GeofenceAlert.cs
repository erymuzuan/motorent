using System.Text.Json.Serialization;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a geofence breach alert.
/// </summary>
public class GeofenceAlert : Entity
{
    public int GeofenceAlertId { get; set; }

    /// <summary>
    /// The geofence that was breached.
    /// </summary>
    public int GeofenceId { get; set; }

    /// <summary>
    /// The vehicle that breached the geofence.
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// Active rental ID at time of breach (if any).
    /// </summary>
    public int? RentalId { get; set; }

    /// <summary>
    /// The GPS position that triggered the alert.
    /// </summary>
    public int? GpsPositionId { get; set; }

    /// <summary>
    /// Whether vehicle entered or exited the zone.
    /// </summary>
    public GeofenceAlertType AlertType { get; set; }

    /// <summary>
    /// Priority level of this alert.
    /// </summary>
    public AlertPriority Priority { get; set; }

    /// <summary>
    /// Current status of the alert.
    /// </summary>
    public AlertStatus Status { get; set; } = AlertStatus.Active;

    /// <summary>
    /// When the breach occurred.
    /// </summary>
    public DateTimeOffset AlertTimestamp { get; set; }

    /// <summary>
    /// Location at time of breach - latitude.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Location at time of breach - longitude.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Human-readable location description.
    /// </summary>
    public string? LocationDescription { get; set; }

    /// <summary>
    /// Whether LINE notification was sent.
    /// </summary>
    public bool LineNotificationSent { get; set; }

    /// <summary>
    /// LINE message ID if sent.
    /// </summary>
    public string? LineMessageId { get; set; }

    /// <summary>
    /// Who acknowledged the alert.
    /// </summary>
    public string? AcknowledgedBy { get; set; }

    /// <summary>
    /// When the alert was acknowledged.
    /// </summary>
    public DateTimeOffset? AcknowledgedTimestamp { get; set; }

    /// <summary>
    /// Who resolved the alert.
    /// </summary>
    public string? ResolvedBy { get; set; }

    /// <summary>
    /// When the alert was resolved.
    /// </summary>
    public DateTimeOffset? ResolvedTimestamp { get; set; }

    /// <summary>
    /// Resolution notes.
    /// </summary>
    public string? ResolutionNotes { get; set; }

    // Denormalized fields for display

    /// <summary>
    /// Geofence name for display.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GeofenceName { get; set; }

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

    /// <summary>
    /// Renter name for display.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RenterName { get; set; }

    /// <summary>
    /// Renter phone for contact.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RenterPhone { get; set; }

    public override int GetId() => this.GeofenceAlertId;
    public override void SetId(int value) => this.GeofenceAlertId = value;

    /// <summary>
    /// Gets the Google Maps URL for the breach location.
    /// </summary>
    [JsonIgnore]
    public string GoogleMapsUrl => $"https://www.google.com/maps?q={Latitude},{Longitude}";

    /// <summary>
    /// Gets the LatLng representation of the breach location.
    /// </summary>
    [JsonIgnore]
    public Spatial.LatLng Location => new(Latitude, Longitude);

    /// <summary>
    /// Whether this alert is still active (not resolved).
    /// </summary>
    [JsonIgnore]
    public bool IsActive => Status == AlertStatus.Active || Status == AlertStatus.Acknowledged;

    /// <summary>
    /// Gets a display string for the alert type.
    /// </summary>
    [JsonIgnore]
    public string AlertTypeDisplay => AlertType switch
    {
        GeofenceAlertType.Enter => "Entered zone",
        GeofenceAlertType.Exit => "Left zone",
        _ => AlertType.ToString()
    };
}

/// <summary>
/// Type of geofence breach.
/// </summary>
public enum GeofenceAlertType
{
    /// <summary>Vehicle entered the geofence zone.</summary>
    Enter,
    /// <summary>Vehicle exited the geofence zone.</summary>
    Exit
}

/// <summary>
/// Status of a geofence alert.
/// </summary>
public enum AlertStatus
{
    /// <summary>Alert is active and unacknowledged.</summary>
    Active,
    /// <summary>Alert has been acknowledged but not resolved.</summary>
    Acknowledged,
    /// <summary>Alert has been resolved.</summary>
    Resolved,
    /// <summary>Alert was marked as a false alarm.</summary>
    FalseAlarm
}
