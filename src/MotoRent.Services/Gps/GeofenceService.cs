using Microsoft.Extensions.Logging;
using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services.Gps;

/// <summary>
/// Manages geofences and breach detection.
/// </summary>
public partial class GeofenceService(
    RentalDataContext context,
    ILogger<GeofenceService> logger)
{
    private RentalDataContext Context { get; } = context;
    private ILogger<GeofenceService> Logger { get; } = logger;

    /// <summary>
    /// Check if a position is inside a geofence.
    /// Delegates to Geofence.ContainsPoint().
    /// </summary>
    public bool IsInsideGeofence(Geofence geofence, double latitude, double longitude)
    {
        return geofence.ContainsPoint(latitude, longitude);
    }
}

/// <summary>
/// Represents a detected geofence breach.
/// </summary>
public class GeofenceBreach
{
    public Geofence Geofence { get; set; } = null!;
    public GeofenceAlertType AlertType { get; set; }
    public AlertPriority Priority { get; set; }
}
