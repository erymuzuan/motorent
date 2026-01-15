using System.Text.Json.Serialization;
using MotoRent.Domain.Spatial;

namespace MotoRent.Domain.Entities;

/// <summary>
/// Represents a geofence zone (polygon or circle).
/// Supports pre-configured Thai province templates and custom shapes.
/// </summary>
public class Geofence : Entity
{
    public int GeofenceId { get; set; }

    /// <summary>
    /// Shop that owns this geofence. Null for global templates.
    /// </summary>
    public int? ShopId { get; set; }

    /// <summary>
    /// Display name (e.g., "Phuket Province", "Shop Safe Zone").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description or purpose of this geofence.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Type of geofence.
    /// </summary>
    public GeofenceType GeofenceType { get; set; } = GeofenceType.Custom;

    /// <summary>
    /// Shape type: Polygon or Circle.
    /// </summary>
    public GeofenceShape Shape { get; set; } = GeofenceShape.Polygon;

    /// <summary>
    /// For Polygon: List of coordinates defining the boundary.
    /// </summary>
    public List<LatLng> Coordinates { get; set; } = [];

    /// <summary>
    /// For Circle: Center point latitude.
    /// </summary>
    public double? CenterLatitude { get; set; }

    /// <summary>
    /// For Circle: Center point longitude.
    /// </summary>
    public double? CenterLongitude { get; set; }

    /// <summary>
    /// For Circle: Radius in meters.
    /// </summary>
    public double? RadiusMeters { get; set; }

    /// <summary>
    /// Alert priority when this geofence is breached.
    /// </summary>
    public AlertPriority AlertPriority { get; set; } = AlertPriority.Medium;

    /// <summary>
    /// Whether entering this zone triggers an alert (vs exiting).
    /// </summary>
    public bool AlertOnEnter { get; set; }

    /// <summary>
    /// Whether exiting this zone triggers an alert.
    /// </summary>
    public bool AlertOnExit { get; set; } = true;

    /// <summary>
    /// Whether to send LINE notification on breach.
    /// </summary>
    public bool SendLineNotification { get; set; } = true;

    /// <summary>
    /// Whether to send in-app notification on breach.
    /// </summary>
    public bool SendInAppNotification { get; set; } = true;

    /// <summary>
    /// Whether this geofence is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a system template (not editable by shop).
    /// </summary>
    public bool IsTemplate { get; set; }

    /// <summary>
    /// Thai province code for province templates.
    /// </summary>
    public string? ProvinceCode { get; set; }

    /// <summary>
    /// Color for map display (hex).
    /// </summary>
    public string? MapColor { get; set; }

    public override int GetId() => this.GeofenceId;
    public override void SetId(int value) => this.GeofenceId = value;

    /// <summary>
    /// Gets the center point for this geofence.
    /// For circles, returns the defined center.
    /// For polygons, returns the centroid.
    /// </summary>
    [JsonIgnore]
    public LatLng? Center
    {
        get
        {
            if (Shape == GeofenceShape.Circle && CenterLatitude.HasValue && CenterLongitude.HasValue)
                return new LatLng(CenterLatitude.Value, CenterLongitude.Value);

            if (Shape == GeofenceShape.Polygon && Coordinates.Count > 0)
            {
                var avgLat = Coordinates.Average(c => c.Lat);
                var avgLng = Coordinates.Average(c => c.Lng);
                return new LatLng(avgLat, avgLng);
            }

            return null;
        }
    }

    /// <summary>
    /// Checks if a point is inside this geofence.
    /// </summary>
    public bool ContainsPoint(double latitude, double longitude)
    {
        if (Shape == GeofenceShape.Circle)
            return ContainsPointCircle(latitude, longitude);

        return ContainsPointPolygon(latitude, longitude);
    }

    /// <summary>
    /// Check if point is inside circle using Haversine distance.
    /// </summary>
    private bool ContainsPointCircle(double latitude, double longitude)
    {
        if (!CenterLatitude.HasValue || !CenterLongitude.HasValue || !RadiusMeters.HasValue)
            return false;

        var center = new LatLng(CenterLatitude.Value, CenterLongitude.Value);
        var point = new LatLng(latitude, longitude);
        var distanceKm = center.GetDistanceKm(point);

        if (!distanceKm.HasValue)
            return false;

        // Convert radius to km for comparison
        var radiusKm = RadiusMeters.Value / 1000.0;
        return distanceKm.Value <= radiusKm;
    }

    /// <summary>
    /// Check if point is inside polygon using ray-casting algorithm.
    /// </summary>
    private bool ContainsPointPolygon(double latitude, double longitude)
    {
        if (Coordinates.Count < 3)
            return false;

        var inside = false;
        var j = Coordinates.Count - 1;

        for (var i = 0; i < Coordinates.Count; i++)
        {
            var xi = Coordinates[i].Lng;
            var yi = Coordinates[i].Lat;
            var xj = Coordinates[j].Lng;
            var yj = Coordinates[j].Lat;

            if (((yi > latitude) != (yj > latitude)) &&
                (longitude < (xj - xi) * (latitude - yi) / (yj - yi) + xi))
            {
                inside = !inside;
            }

            j = i;
        }

        return inside;
    }
}

/// <summary>
/// Type of geofence zone.
/// </summary>
public enum GeofenceType
{
    /// <summary>Thai province boundary.</summary>
    Province,
    /// <summary>Tourist safe zone (e.g., Patong area).</summary>
    SafeZone,
    /// <summary>Shop vicinity zone.</summary>
    ShopZone,
    /// <summary>Airport/ferry pickup zone.</summary>
    ServiceZone,
    /// <summary>Prohibited/dangerous road.</summary>
    ProhibitedZone,
    /// <summary>Custom user-defined zone.</summary>
    Custom
}

/// <summary>
/// Shape of the geofence boundary.
/// </summary>
public enum GeofenceShape
{
    /// <summary>Polygon defined by coordinate vertices.</summary>
    Polygon,
    /// <summary>Circle defined by center point and radius.</summary>
    Circle
}

/// <summary>
/// Priority level for alerts.
/// </summary>
public enum AlertPriority
{
    /// <summary>Low priority - informational.</summary>
    Low,
    /// <summary>Medium priority - requires attention.</summary>
    Medium,
    /// <summary>High priority - immediate attention needed.</summary>
    High,
    /// <summary>Critical - emergency response required.</summary>
    Critical
}
