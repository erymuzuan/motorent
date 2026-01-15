using System.Text.Json.Serialization;
using MotoRent.Domain.Spatial;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Aggregated trip history for a vehicle rental period.
/// Generated from GPS positions for analysis and reporting.
/// </summary>
public class TripHistory : Entity
{
    public int TripHistoryId { get; set; }

    /// <summary>
    /// The vehicle this trip is for.
    /// </summary>
    public int VehicleId { get; set; }

    /// <summary>
    /// The rental this trip is associated with (optional).
    /// </summary>
    public int? RentalId { get; set; }

    /// <summary>
    /// Trip start time.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }

    /// <summary>
    /// Trip end time.
    /// </summary>
    public DateTimeOffset EndTime { get; set; }

    /// <summary>
    /// Starting location.
    /// </summary>
    public LatLng? StartLocation { get; set; }

    /// <summary>
    /// Ending location.
    /// </summary>
    public LatLng? EndLocation { get; set; }

    /// <summary>
    /// Total distance in kilometers.
    /// </summary>
    public double TotalDistanceKm { get; set; }

    /// <summary>
    /// Total moving time in minutes.
    /// </summary>
    public int MovingTimeMinutes { get; set; }

    /// <summary>
    /// Total idle/stopped time in minutes.
    /// </summary>
    public int IdleTimeMinutes { get; set; }

    /// <summary>
    /// Maximum speed recorded during trip.
    /// </summary>
    public double MaxSpeedKmh { get; set; }

    /// <summary>
    /// Average speed during movement.
    /// </summary>
    public double AverageSpeedKmh { get; set; }

    /// <summary>
    /// Estimated fuel consumed in liters.
    /// </summary>
    public double? EstimatedFuelLiters { get; set; }

    /// <summary>
    /// Number of harsh braking events detected.
    /// </summary>
    public int HarshBrakingCount { get; set; }

    /// <summary>
    /// Number of harsh acceleration events.
    /// </summary>
    public int HarshAccelerationCount { get; set; }

    /// <summary>
    /// Number of overspeed events.
    /// </summary>
    public int OverspeedCount { get; set; }

    /// <summary>
    /// Number of geofence violations during trip.
    /// </summary>
    public int GeofenceViolationCount { get; set; }

    /// <summary>
    /// Encoded polyline of the trip route for map rendering.
    /// </summary>
    public string? RoutePolyline { get; set; }

    /// <summary>
    /// List of behavior flags and events during the trip.
    /// </summary>
    public List<TripBehaviorFlag> BehaviorFlags { get; set; } = [];

    // Denormalized fields for display

    /// <summary>
    /// Vehicle license plate for display.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? VehicleLicensePlate { get; set; }

    /// <summary>
    /// Renter name for display.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RenterName { get; set; }

    public override int GetId() => this.TripHistoryId;
    public override void SetId(int value) => this.TripHistoryId = value;

    /// <summary>
    /// Gets the total trip duration.
    /// </summary>
    [JsonIgnore]
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Gets the total time in minutes.
    /// </summary>
    [JsonIgnore]
    public int TotalTimeMinutes => MovingTimeMinutes + IdleTimeMinutes;

    /// <summary>
    /// Gets the total number of behavior events.
    /// </summary>
    [JsonIgnore]
    public int TotalBehaviorEvents => HarshBrakingCount + HarshAccelerationCount + OverspeedCount;

    /// <summary>
    /// Whether this trip has any behavior warnings.
    /// </summary>
    [JsonIgnore]
    public bool HasBehaviorWarnings => TotalBehaviorEvents > 0 || GeofenceViolationCount > 0;

    /// <summary>
    /// Gets a summary of the trip for display.
    /// </summary>
    [JsonIgnore]
    public string Summary => $"{TotalDistanceKm:F1} km in {Duration.TotalHours:F1} hours";
}

/// <summary>
/// Represents a behavior flag or event during a trip.
/// </summary>
public class TripBehaviorFlag
{
    /// <summary>
    /// Type of behavior event.
    /// </summary>
    public BehaviorFlagType Type { get; set; }

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Location where the event occurred.
    /// </summary>
    public LatLng? Location { get; set; }

    /// <summary>
    /// Additional details (e.g., speed for overspeed events).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Severity of the event.
    /// </summary>
    public BehaviorSeverity Severity { get; set; } = BehaviorSeverity.Warning;
}

/// <summary>
/// Type of driving behavior event.
/// </summary>
public enum BehaviorFlagType
{
    /// <summary>Harsh braking detected.</summary>
    HarshBraking,
    /// <summary>Harsh acceleration detected.</summary>
    HarshAcceleration,
    /// <summary>Exceeded speed limit.</summary>
    Overspeed,
    /// <summary>Geofence breach.</summary>
    GeofenceViolation,
    /// <summary>Extended idle time.</summary>
    ExtendedIdle,
    /// <summary>Vehicle movement after hours.</summary>
    AfterHoursMovement
}

/// <summary>
/// Severity level of behavior event.
/// </summary>
public enum BehaviorSeverity
{
    /// <summary>Informational only.</summary>
    Info,
    /// <summary>Warning - should be noted.</summary>
    Warning,
    /// <summary>Serious - requires attention.</summary>
    Serious
}
