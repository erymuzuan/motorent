namespace MotoRent.Domain.Spatial;

/// <summary>
/// Represents a geographic coordinate with latitude and longitude.
/// Used for GPS locations of shops and service locations.
/// </summary>
public class LatLng
{
    /// <summary>
    /// Latitude in decimal degrees (-90 to 90).
    /// </summary>
    public double Lat { get; set; }

    /// <summary>
    /// Longitude in decimal degrees (-180 to 180).
    /// </summary>
    public double Lng { get; set; }

    /// <summary>
    /// Optional label/name for the location.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// Optional elevation in meters above sea level.
    /// </summary>
    public double? Elevation { get; set; }

    /// <summary>
    /// Calculated distance from another point (km). Set by distance calculations.
    /// </summary>
    public double? Distance { get; set; }

    public LatLng()
    {
    }

    public LatLng(double lat, double lng)
    {
        Lat = lat;
        Lng = lng;
    }

    /// <summary>
    /// Parse from "lat,lng" string format.
    /// </summary>
    public LatLng(string point)
    {
        var parts = point.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            if (double.TryParse(parts[0].Trim(), out var lat))
                Lat = lat;
            if (double.TryParse(parts[1].Trim(), out var lng))
                Lng = lng;
        }
    }

    /// <summary>
    /// Whether this LatLng has valid coordinates (not default 0,0).
    /// </summary>
    public bool HasValue => Math.Abs(Lat) > 0.0001 || Math.Abs(Lng) > 0.0001;

    /// <summary>
    /// Calculate distance to another point using Haversine formula.
    /// </summary>
    /// <param name="point">The other point to measure distance to.</param>
    /// <returns>Distance in kilometers, or null if the other point is invalid.</returns>
    public double? GetDistanceKm(LatLng? point)
    {
        if (point is null || !point.HasValue)
            return null;

        const double earthRadiusKm = 6371.0;

        var dLat = ToRadians(point.Lat - Lat);
        var dLng = ToRadians(point.Lng - Lng);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(Lat)) * Math.Cos(ToRadians(point.Lat)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);

        var c = 2 * Math.Asin(Math.Sqrt(a));

        return earthRadiusKm * c;
    }

    /// <summary>
    /// Convert degrees to radians.
    /// </summary>
    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    /// <summary>
    /// Returns "lat,lng" string format.
    /// </summary>
    public override string ToString() => $"{Lat},{Lng}";

    /// <summary>
    /// Generate Google Maps URL for directions to this location.
    /// </summary>
    public string GetGoogleMapsUrl() =>
        $"https://www.google.com/maps/dir/?api=1&destination={Lat},{Lng}";

    /// <summary>
    /// Generate Google Maps URL to view this location.
    /// </summary>
    public string GetGoogleMapsViewUrl() =>
        $"https://www.google.com/maps?q={Lat},{Lng}";

    /// <summary>
    /// Generate Google Maps URL with a specific zoom level.
    /// </summary>
    public string GetGoogleMapsViewUrl(int zoom) =>
        $"https://www.google.com/maps?q={Lat},{Lng}&z={zoom}";

    /// <summary>
    /// Create a deep copy of this LatLng.
    /// </summary>
    public LatLng Clone() => new(Lat, Lng)
    {
        Label = Label,
        Elevation = Elevation,
        Distance = Distance
    };

    /// <summary>
    /// Default location for Thailand (Phuket area).
    /// </summary>
    public static LatLng PhuketDefault => new(7.8804, 98.3923) { Label = "Phuket" };

    /// <summary>
    /// Default location for Krabi.
    /// </summary>
    public static LatLng KrabiDefault => new(8.0863, 98.9063) { Label = "Krabi" };

    /// <summary>
    /// Default location for Koh Samui.
    /// </summary>
    public static LatLng KohSamuiDefault => new(9.5120, 100.0136) { Label = "Koh Samui" };
}
