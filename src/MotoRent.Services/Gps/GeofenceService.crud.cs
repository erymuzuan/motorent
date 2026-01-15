using MotoRent.Domain.DataContext;
using MotoRent.Domain.Entities;

namespace MotoRent.Services.Gps;

/// <summary>
/// Geofence CRUD operations.
/// </summary>
public partial class GeofenceService
{
    /// <summary>
    /// Get all geofences for a shop (including templates).
    /// </summary>
    public async Task<List<Geofence>> GetGeofencesAsync(int shopId)
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<Geofence>()
                .Where(g => (g.ShopId == shopId || g.IsTemplate) && g.IsActive)
                .OrderBy(g => g.Name),
            1, 500);

        return result.ItemCollection;
    }

    /// <summary>
    /// Get pre-configured Thai province templates.
    /// </summary>
    public async Task<List<Geofence>> GetProvinceTemplatesAsync()
    {
        var result = await this.Context.LoadAsync(
            this.Context.CreateQuery<Geofence>()
                .Where(g => g.IsTemplate && g.GeofenceType == GeofenceType.Province && g.IsActive)
                .OrderBy(g => g.Name),
            1, 100);

        return result.ItemCollection;
    }

    /// <summary>
    /// Get a geofence by ID.
    /// </summary>
    public async Task<Geofence?> GetGeofenceAsync(int geofenceId)
    {
        return await this.Context.LoadOneAsync<Geofence>(g => g.GeofenceId == geofenceId);
    }

    /// <summary>
    /// Create a custom geofence for a shop.
    /// </summary>
    public async Task<SubmitOperation> CreateGeofenceAsync(Geofence geofence, string username)
    {
        // Validate based on shape
        if (geofence.Shape == GeofenceShape.Polygon && geofence.Coordinates.Count < 3)
        {
            return SubmitOperation.CreateFailure("Polygon requires at least 3 coordinates");
        }

        if (geofence.Shape == GeofenceShape.Circle)
        {
            if (!geofence.CenterLatitude.HasValue || !geofence.CenterLongitude.HasValue)
            {
                return SubmitOperation.CreateFailure("Circle requires center coordinates");
            }
            if (!geofence.RadiusMeters.HasValue || geofence.RadiusMeters <= 0)
            {
                return SubmitOperation.CreateFailure("Circle requires positive radius");
            }
        }

        using var session = this.Context.OpenSession(username);
        session.Attach(geofence);

        return await session.SubmitChanges("CreateGeofence");
    }

    /// <summary>
    /// Update a geofence.
    /// </summary>
    public async Task<SubmitOperation> UpdateGeofenceAsync(Geofence geofence, string username)
    {
        using var session = this.Context.OpenSession(username);
        session.Attach(geofence);

        return await session.SubmitChanges("UpdateGeofence");
    }

    /// <summary>
    /// Delete a custom geofence (templates cannot be deleted).
    /// </summary>
    public async Task<SubmitOperation> DeleteGeofenceAsync(int geofenceId, string username)
    {
        var geofence = await this.Context.LoadOneAsync<Geofence>(g => g.GeofenceId == geofenceId);
        if (geofence is null)
        {
            return SubmitOperation.CreateFailure("Geofence not found");
        }

        if (geofence.IsTemplate)
        {
            return SubmitOperation.CreateFailure("Cannot delete template geofences");
        }

        geofence.IsActive = false;

        using var session = this.Context.OpenSession(username);
        session.Attach(geofence);

        return await session.SubmitChanges("DeleteGeofence");
    }

    /// <summary>
    /// Create a shop zone geofence (circle around shop).
    /// </summary>
    public async Task<SubmitOperation> CreateShopZoneAsync(
        int shopId,
        string name,
        double centerLat,
        double centerLng,
        double radiusMeters,
        string username)
    {
        var geofence = new Geofence
        {
            ShopId = shopId,
            Name = name,
            GeofenceType = GeofenceType.ShopZone,
            Shape = GeofenceShape.Circle,
            CenterLatitude = centerLat,
            CenterLongitude = centerLng,
            RadiusMeters = radiusMeters,
            AlertPriority = AlertPriority.Low,
            AlertOnEnter = false,
            AlertOnExit = true,
            SendLineNotification = false,
            SendInAppNotification = true,
            IsActive = true,
            MapColor = "#4CAF50" // Green
        };

        return await this.CreateGeofenceAsync(geofence, username);
    }
}
